import uuid
from typing import Optional
from fastapi import Header

def get_correlation_id(x_correlation_id: Optional[str] = Header(None)):
    """
    Ensure every incoming request has a correlation id.
    Used for logging & tracing across microservices.
    """
    return x_correlation_id or str(uuid.uuid4())
