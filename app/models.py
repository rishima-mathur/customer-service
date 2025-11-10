from sqlalchemy import Column, Integer, String, DateTime, ForeignKey
from sqlalchemy.orm import declarative_base, relationship
from datetime import datetime

Base = declarative_base()

class Customer(Base):
    __tablename__ = "customers"

    customer_id = Column(Integer, primary_key=True, index=True)
    name = Column(String(255), nullable=False)
    email = Column(String(255), unique=True, nullable=False, index=True)
    phone = Column(String(20), unique=True, nullable=False, index=True)
    created_at = Column(DateTime, default=datetime.utcnow)

    addresses = relationship("Address", back_populates="customer")


class Address(Base):
    __tablename__ = "addresses"

    address_id = Column(Integer, primary_key=True, index=True)
    customer_id = Column(Integer, ForeignKey("customers.customer_id"), nullable=False)
    line1 = Column(String(255), nullable=False)
    area = Column(String(255), nullable=True)
    city = Column(String(100), nullable=False)
    pincode = Column(String(20), nullable=False)
    created_at = Column(DateTime, default=datetime.utcnow)

    customer = relationship("Customer", back_populates="addresses")
