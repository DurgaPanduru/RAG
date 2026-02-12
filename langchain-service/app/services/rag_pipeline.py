from typing import List, Dict, Any
from langchain_anthropic import ChatAnthropic
from langchain_chroma import Chroma
from langchain.embeddings import HuggingFaceEmbeddings
from langchain.schema import Document
import structlog

from app.config import get_settings
from app.services.document_processor import DocumentProcessor

logger = structlog.get_logger()
settings = get_settings()


class RAGPipeline:
    def __init__(self):
        # Initialize embeddings
        logger.info("initializing_embeddings", model=settings.embedding_model)
        self.embeddings = HuggingFaceEmbeddings(
            model_name=settings.embedding_model,
            model_kwargs={"device": "cpu"},
            encode_kwargs={"normalize_embeddings": True}
        )

        # Initialize vector store
        logger.info("initializing_vector_store", host=settings.chroma_host)
        self.vectorstore = Chroma(
            collection_name=settings.chroma_collection_name,
            embedding_function=self.embeddings,
            client_settings={
                "host": settings.chroma_host,
                "port": settings.chroma_port
            }
        )

        # Initialize Claude
        logger.info("initializing_claude", model=settings.claude_model_primary)
        self.llm = ChatAnthropic(
            model=settings.claude_model_primary,
            anthropic_api_key=settings.anthropic_api_key,
            max_tokens=settings.claude_max_tokens,
            temperature=settings.claude_temperature,
            timeout=settings.claude_timeout_seconds
        )

        # Initialize document processor
        self.document_processor = DocumentProcessor()

        logger.info("rag_pipeline_initialized")

    async def process_document(
        self,
        document_id: str,
        document_content: bytes,
        content_type: str,
        filename: str
    ) -> Dict[str, Any]:
        """
        Process and index a document.
        """
        logger.info(
            "processing_document",
            document_id=document_id,
            filename=filename,
            content_type=content_type
        )

        try:
            # Process PDF
            chunks, page_count = await self.document_processor.process_pdf(
                document_content,
                document_id,
                filename
            )

            # Add to vector store
            logger.debug("adding_to_vectorstore", chunks=len(chunks))
            self.vectorstore.add_documents(chunks)

            logger.info(
                "document_indexed",
                document_id=document_id,
                chunks=len(chunks),
                pages=page_count
            )

            return {
                "success": True,
                "message": f"Processed {page_count} pages into {len(chunks)} chunks",
                "chunks_created": len(chunks)
            }

        except Exception as e:
            logger.error(
                "document_processing_failed",
                document_id=document_id,
                error=str(e)
            )
            return {
                "success": False,
                "message": f"Error processing document: {str(e)}",
                "chunks_created": 0
            }

    async def query(
        self,
        query: str,
        top_k: int = 5,
        conversation_id: str = None
    ) -> Dict[str, Any]:
        """
        Query the RAG system.
        """
        logger.info("querying_rag", query=query[:100], top_k=top_k)

        try:
            # Retrieve relevant chunks
            logger.debug("retrieving_relevant_chunks")
            results = self.vectorstore.similarity_search_with_score(
                query,
                k=top_k
            )

            if not results:
                logger.warning("no_relevant_chunks_found")
                return {
                    "answer": "I couldn't find any relevant information in the documents to answer your question.",
                    "sources": [],
                    "tokens_used": 0
                }

            # Build context from retrieved chunks
            context_parts = []
            sources = []

            for idx, (doc, score) in enumerate(results):
                context_parts.append(f"[Source {idx + 1}]\n{doc.page_content}\n")

                sources.append({
                    "document_id": doc.metadata.get("document_id", "unknown"),
                    "chunk_id": doc.metadata.get("chunk_id", "unknown"),
                    "page_number": doc.metadata.get("page_number", 0),
                    "relevance_score": float(1 - score),  # Convert distance to similarity
                    "preview": self.document_processor.get_chunk_preview(doc.page_content)
                })

            context = "\n\n".join(context_parts)

            # Build prompt
            prompt = f"""You are a helpful AI assistant. Use the following context from documents to answer the user's question.
If the answer cannot be found in the context, say so honestly.

Context:
{context}

Question: {query}

Answer:"""

            # Get response from Claude
            logger.debug("calling_claude")
            response = await self.llm.ainvoke(prompt)

            answer = response.content

            # Calculate approximate token usage
            tokens_used = len(prompt.split()) + len(answer.split())

            logger.info(
                "query_completed",
                sources_found=len(sources),
                tokens_used=tokens_used
            )

            return {
                "answer": answer,
                "sources": sources,
                "tokens_used": tokens_used
            }

        except Exception as e:
            logger.error("query_failed", error=str(e))
            raise
