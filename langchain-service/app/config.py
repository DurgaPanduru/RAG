from pydantic_settings import BaseSettings
from functools import lru_cache


class Settings(BaseSettings):
    # Application
    app_name: str = "RAG LangChain Service"
    app_version: str = "1.0.0"
    environment: str = "development"
    debug: bool = False

    # API Keys
    anthropic_api_key: str

    # ChromaDB
    chroma_host: str = "chroma"
    chroma_port: int = 8000
    chroma_collection_name: str = "documents"

    # Redis
    redis_url: str = "redis://redis:6379"

    # Claude Models
    claude_model_primary: str = "claude-sonnet-4-5"
    claude_model_guardrails: str = "claude-haiku-4-5"
    claude_max_tokens: int = 2048
    claude_temperature: float = 0.7
    claude_timeout_seconds: int = 60

    # RAG Configuration
    chunk_size: int = 1000
    chunk_overlap: int = 200
    top_k_results: int = 5

    # Embedding model
    embedding_model: str = "sentence-transformers/all-MiniLM-L6-v2"

    # Logging
    log_level: str = "INFO"

    class Config:
        env_file = ".env"
        case_sensitive = False


@lru_cache()
def get_settings() -> Settings:
    return Settings()
