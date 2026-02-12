import io
from typing import List, Tuple
from pypdf import PdfReader
from langchain.text_splitter import RecursiveCharacterTextSplitter
from langchain.schema import Document
import structlog

from app.config import get_settings

logger = structlog.get_logger()
settings = get_settings()


class DocumentProcessor:
    def __init__(self):
        self.text_splitter = RecursiveCharacterTextSplitter(
            chunk_size=settings.chunk_size,
            chunk_overlap=settings.chunk_overlap,
            length_function=len,
            separators=["\n\n", "\n", " ", ""]
        )

    async def process_pdf(
        self,
        document_content: bytes,
        document_id: str,
        filename: str
    ) -> Tuple[List[Document], int]:
        """
        Process PDF document and return chunks with metadata.

        Args:
            document_content: PDF file bytes
            document_id: Unique document identifier
            filename: Original filename

        Returns:
            Tuple of (list of Document chunks, page count)
        """
        logger.info("processing_pdf", document_id=document_id, filename=filename)

        try:
            # Read PDF
            pdf_file = io.BytesIO(document_content)
            pdf_reader = PdfReader(pdf_file)
            page_count = len(pdf_reader.pages)

            logger.debug("pdf_loaded", pages=page_count)

            # Extract text from all pages
            all_text = []
            for page_num, page in enumerate(pdf_reader.pages, start=1):
                text = page.extract_text()
                if text.strip():
                    all_text.append({
                        "text": text,
                        "page": page_num
                    })

            # Create documents with metadata
            documents = []
            for page_data in all_text:
                doc = Document(
                    page_content=page_data["text"],
                    metadata={
                        "document_id": document_id,
                        "filename": filename,
                        "page_number": page_data["page"],
                        "total_pages": page_count
                    }
                )
                documents.append(doc)

            # Split into chunks
            chunks = self.text_splitter.split_documents(documents)

            # Add chunk index to metadata
            for idx, chunk in enumerate(chunks):
                chunk.metadata["chunk_index"] = idx
                chunk.metadata["chunk_id"] = f"{document_id}_chunk_{idx}"

            logger.info(
                "pdf_processed",
                document_id=document_id,
                pages=page_count,
                chunks=len(chunks)
            )

            return chunks, page_count

        except Exception as e:
            logger.error(
                "pdf_processing_error",
                document_id=document_id,
                error=str(e)
            )
            raise


    def get_chunk_preview(self, text: str, max_length: int = 150) -> str:
        """Get a preview of the chunk text"""
        if len(text) <= max_length:
            return text
        return text[:max_length] + "..."
