using AutoMapper;
using ProcureLive.Contracts.Catalog;
using ProcureLive.Contracts.Companies;
using ProcureLive.Contracts.Finance;
using ProcureLive.Domain.Entities.Catalog;
using ProcureLive.Domain.Entities.Companies;
using ProcureLive.Domain.Entities.Finance;

namespace ProcureLive.Application.Common.Mappings;

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Company, CompanyDto>()
            .ForMember(d => d.CompanyType, opt => opt.MapFrom(s => s.CompanyType.ToString()))
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()));

        CreateMap<Company, CompanyDetailDto>()
            .ForMember(d => d.CompanyType, opt => opt.MapFrom(s => s.CompanyType.ToString()))
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.ContactPersons, opt => opt.MapFrom(s => s.ContactPersons))
            .ForMember(d => d.Addresses, opt => opt.MapFrom(s => s.Addresses));

        CreateMap<ContactPerson, ContactPersonDto>();

        CreateMap<Address, AddressDto>()
            .ForMember(d => d.AddressType, opt => opt.MapFrom(s => s.AddressType.ToString()));

        CreateMap<Category, CategoryDto>()
            .ForMember(d => d.Children, opt => opt.MapFrom(s => s.ChildCategories));

        CreateMap<Product, ProductDto>()
            .ForMember(d => d.CategoryName, opt => opt.MapFrom(s => s.Category.Name))
            .ForMember(d => d.UnitOfMeasure, opt => opt.MapFrom(s => s.UnitOfMeasure.ToString()))
            .ForMember(d => d.Images, opt => opt.MapFrom(s => s.Images));

        CreateMap<ProductImage, ProductImageDto>();

        CreateMap<Currency, CurrencyDto>();

        CreateMap<ExchangeRate, ExchangeRateDto>()
            .ForMember(d => d.CurrencyCode, opt => opt.MapFrom(s => s.Currency.Code));
    }
}
