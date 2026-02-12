from fastapi import APIRouter, UploadFile, File, HTTPException
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


@router.post("/process")
async def process_document(file: UploadFile = File(...)):
    """Process and index a PDF document"""
    if not file.filename.endswith(".pdf"):
        raise HTTPException(status_code=400, detail="Only PDF files are supported")

    try:
        content = await file.read()
        document_id = file.filename.replace(".pdf", "")

        pipeline = get_rag_pipeline()
        result = await pipeline.process_document(
            document_id=document_id,
            document_content=content,
            content_type="application/pdf",
            filename=file.filename
        )

        return result

    except Exception as e:
        logger.error("document_upload_failed", error=str(e))
        raise HTTPException(status_code=500, detail=str(e))
