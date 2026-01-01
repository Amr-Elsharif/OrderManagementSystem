using AutoMapper;
using OrderManagementSystem.Application.DTOs.Customers;
using OrderManagementSystem.Application.DTOs.Orders;
using OrderManagementSystem.Application.DTOs.Products;
using OrderManagementSystem.Domain.Entities;

namespace OrderManagementSystem.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Order, OrderDto>()
                .ForMember(dest => dest.OrderNumber, opt => opt.MapFrom(src => src.OrderNumber))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.TotalAmount, opt => opt.MapFrom(src => src.TotalAmount.Amount))
                .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.TotalAmount.Currency))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));

            CreateMap<OrderItem, OrderItemDto>();
            CreateMap<Product, ProductDto>();
            CreateMap<Customer, CustomerDto>();
        }
    }
}