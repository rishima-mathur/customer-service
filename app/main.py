from fastapi import FastAPI, Depends, HTTPException, Query
from sqlalchemy.orm import Session
from typing import List, Optional
import logging

from . import db, models, schemas
from .metrics import MetricsMiddleware, metrics_endpoint
from .deps import get_correlation_id

# ----- Logging -----
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] [customer-service] [cid=%(correlation_id)s] %(message)s",
)
logger = logging.getLogger("customer-service")

# ----- Init -----
db.init_db()
app = FastAPI(title="customer-service", version="v1")
app.add_middleware(MetricsMiddleware, service_name="customer-service")
 

# ----- DB Dependency -----
def get_db():
    session = db.SessionLocal()
    try:
        yield session
    finally:
        session.close()


# ----- Infra Endpoints -----
@app.get("/health")
def health():
    return {"status": "ok", "service": "customer-service"}


@app.get("/metrics")
def metrics():
    return metrics_endpoint()


# ----- Customer APIs -----


@app.post("/v1/customers", response_model=schemas.CustomerRead, status_code=201)
def create_customer(
    payload: schemas.CustomerCreate,
    db_sess: Session = Depends(get_db),
    cid: str = Depends(get_correlation_id),
):
    # Uniqueness enforcement (business rule)
    existing = (
        db_sess.query(models.Customer)
        .filter(
            (models.Customer.email == payload.email)
            | (models.Customer.phone == payload.phone)
        )
        .first()
    )
    if existing:
        raise HTTPException(
            status_code=400,
            detail={
                "code": "CUSTOMER_EXISTS",
                "message": "Customer with this email or phone already exists",
                "correlationId": cid,
            },
        )

    c = models.Customer(**payload.dict())
    db_sess.add(c)
    db_sess.commit()
    db_sess.refresh(c)

    logger.info("Customer created", extra={"correlation_id": cid})
    return c


@app.get("/v1/customers", response_model=List[schemas.CustomerRead])
def list_customers(
    db_sess: Session = Depends(get_db),
    cid: str = Depends(get_correlation_id),
    name: Optional[str] = Query(None),
    email: Optional[str] = Query(None),
    phone: Optional[str] = Query(None),
    limit: int = Query(50, ge=1, le=200),
    offset: int = Query(0, ge=0),
):
    """
    Search / list customers.
    Used for admin/reporting, but also proves richer API.
    """
    q = db_sess.query(models.Customer)

    if name:
        q = q.filter(models.Customer.name.ilike(f"%{name}%"))
    if email:
        q = q.filter(models.Customer.email == email)
    if phone:
        q = q.filter(models.Customer.phone == phone)

    customers = q.order_by(models.Customer.customer_id).offset(offset).limit(limit).all()
    logger.info("Listed customers", extra={"correlation_id": cid})
    return customers


@app.get("/v1/customers/{customer_id}", response_model=schemas.CustomerWithAddresses)
def get_customer(
    customer_id: int,
    db_sess: Session = Depends(get_db),
    cid: str = Depends(get_correlation_id),
):
    c = (
        db_sess.query(models.Customer)
        .filter(models.Customer.customer_id == customer_id)
        .first()
    )
    if not c:
        raise HTTPException(
            status_code=404,
            detail={
                "code": "CUSTOMER_NOT_FOUND",
                "message": "Customer not found",
                "correlationId": cid,
            },
        )
    return c


@app.patch("/v1/customers/{customer_id}", response_model=schemas.CustomerRead)
def update_customer(
    customer_id: int,
    payload: schemas.CustomerUpdate,
    db_sess: Session = Depends(get_db),
    cid: str = Depends(get_correlation_id),
):
    c = (
        db_sess.query(models.Customer)
        .filter(models.Customer.customer_id == customer_id)
        .first()
    )
    if not c:
        raise HTTPException(
            status_code=404,
            detail={
                "code": "CUSTOMER_NOT_FOUND",
                "correlationId": cid,
            },
        )

    data = payload.dict(exclude_unset=True)
    # small rule: cannot update email through this API (protect login identity)
    if "name" in data:
        c.name = data["name"]
    if "phone" in data:
        # ensure new phone is unique
        exists = (
            db_sess.query(models.Customer)
            .filter(models.Customer.phone == data["phone"],
                    models.Customer.customer_id != customer_id)
            .first()
        )
        if exists:
            raise HTTPException(
                400,
                {
                    "code": "PHONE_IN_USE",
                    "message": "Phone already associated with another customer",
                    "correlationId": cid,
                },
            )
        c.phone = data["phone"]

    db_sess.commit()
    db_sess.refresh(c)
    logger.info("Customer updated", extra={"correlation_id": cid})
    return c


@app.delete("/v1/customers/{customer_id}", status_code=204)
def delete_customer(
    customer_id: int,
    db_sess: Session = Depends(get_db),
    cid: str = Depends(get_correlation_id),
):
    c = (
        db_sess.query(models.Customer)
        .filter(models.Customer.customer_id == customer_id)
        .first()
    )
    if not c:
        raise HTTPException(
            status_code=404,
            detail={"code": "CUSTOMER_NOT_FOUND", "correlationId": cid},
        )
    db_sess.delete(c)
    db_sess.commit()
    logger.info("Customer deleted", extra={"correlation_id": cid})
    return


# ----- Address APIs -----


@app.get("/v1/customers/{customer_id}/addresses", response_model=List[schemas.AddressRead])
def list_addresses(
    customer_id: int,
    db_sess: Session = Depends(get_db),
    cid: str = Depends(get_correlation_id),
):
    # validate customer exists (tiny optimization: join or direct query)
    exists = (
        db_sess.query(models.Customer)
        .filter(models.Customer.customer_id == customer_id)
        .first()
    )
    if not exists:
        raise HTTPException(404, {"code": "CUSTOMER_NOT_FOUND", "correlationId": cid})

    addrs = (
        db_sess.query(models.Address)
        .filter(models.Address.customer_id == customer_id)
        .order_by(models.Address.address_id)
        .all()
    )
    return addrs


@app.post("/v1/customers/{customer_id}/addresses", response_model=schemas.AddressRead, status_code=201)
def add_address(
    customer_id: int,
    payload: schemas.AddressCreate,
    db_sess: Session = Depends(get_db),
    cid: str = Depends(get_correlation_id),
):
    c = (
        db_sess.query(models.Customer)
        .filter(models.Customer.customer_id == customer_id)
        .first()
    )
    if not c:
        raise HTTPException(404, {"code": "CUSTOMER_NOT_FOUND", "correlationId": cid})

    addr = models.Address(customer_id=customer_id, **payload.dict())
    db_sess.add(addr)
    db_sess.commit()
    db_sess.refresh(addr)

    logger.info(
        "Address created",
        extra={"correlation_id": cid},
    )
    return addr


# ----- Internal Helper Endpoint for Other Services -----
# Used by order-service to validate that a (customer_id, address_id) pair is valid.

@app.get("/internal/v1/customers/{customer_id}/validate-address")
def validate_customer_address(
    customer_id: int,
    address_id: int,
    db_sess: Session = Depends(get_db),
    cid: str = Depends(get_correlation_id),
):
    c = (
        db_sess.query(models.Customer)
        .filter(models.Customer.customer_id == customer_id)
        .first()
    )
    if not c:
        return {"valid": False, "reason": "CUSTOMER_NOT_FOUND", "correlationId": cid}

    a = (
        db_sess.query(models.Address)
        .filter(
            models.Address.customer_id == customer_id,
            models.Address.address_id == address_id,
        )
        .first()
    )
    if not a:
        return {"valid": False, "reason": "ADDRESS_NOT_OWNED", "correlationId": cid}

    return {"valid": True, "correlationId": cid}
