from pydantic import BaseModel, EmailStr
from typing import Optional, List

class AddressBase(BaseModel):
    line1: str
    area: Optional[str] = None
    city: str
    pincode: str

class AddressCreate(AddressBase):
    pass

class AddressRead(AddressBase):
    address_id: int
    customer_id: int

    class Config:
        from_attributes = True


class CustomerBase(BaseModel):
    name: str
    email: EmailStr
    phone: str

class CustomerCreate(CustomerBase):
    pass

class CustomerUpdate(BaseModel):
    name: Optional[str] = None
    phone: Optional[str] = None

class CustomerRead(CustomerBase):
    customer_id: int

    class Config:
        from_attributes = True


class CustomerWithAddresses(CustomerRead):
    addresses: List[AddressRead] = []
