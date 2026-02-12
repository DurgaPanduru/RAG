from fastapi import APIRouter, HTTPException
from pydantic import BaseModel
from app.services.rag_pipeline import RAGPipeline
import structlog

router = APIRouter()
logger = structlog.get_logger()

# Global RAG pipeline instance
rag_pipeline = None


def get_rag_pipeline():
    global rag_pipeline
    if rag_pipeline is None:
        rag_pipeline = RAGPipeline()
    return rag_pipeline


class QueryRequest(BaseModel):
    query: str
    top_k: int = 5
    conversation_id: str | None = None


@router.post("/query")
async def query_rag(request: QueryRequest):
    """Query the RAG system"""
    try:
        pipeline = get_rag_pipeline()
        result = await pipeline.query(
            query=request.query,
            top_k=request.top_k,
            conversation_id=request.conversation_id
        )
        return result

    except Exception as e:
        logger.error("query_failed", error=str(e))
        raise HTTPException(status_code=500, detail=str(e))
