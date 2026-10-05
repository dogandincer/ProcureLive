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
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.WorkspaceName, opt => opt.MapFrom(s => s.Workspace != null ? s.Workspace.Name : null));

        CreateMap<Company, CompanyDetailDto>()
            .ForMember(d => d.CompanyType, opt => opt.MapFrom(s => s.CompanyType.ToString()))
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.ContactPersons, opt => opt.MapFrom(s => s.ContactPersons))
            .ForMember(d => d.Addresses, opt => opt.MapFrom(s => s.Addresses))
            .ForMember(d => d.Sectors, opt => opt.MapFrom(s => s.Sectors))
            .ForMember(d => d.WorkspaceName, opt => opt.MapFrom(s => s.Workspace != null ? s.Workspace.Name : null));

        CreateMap<CompanySector, SectorDto>()
            .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Sector.Id))
            .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Sector.Name))
            .ForMember(d => d.Description, opt => opt.MapFrom(s => s.Sector.Description))
            .ForMember(d => d.IsActive, opt => opt.MapFrom(s => s.Sector.IsActive))
            .ForMember(d => d.CompanyCount, opt => opt.MapFrom(s => 0));

        CreateMap<ContactPerson, ContactPersonDto>();

        CreateMap<Address, AddressDto>()
            .ForMember(d => d.AddressType, opt => opt.MapFrom(s => s.AddressType.ToString()));

        CreateMap<Category, CategoryDto>()
            .ForMember(d => d.Children, opt => opt.MapFrom(s => s.ChildCategories));

        CreateMap<Product, ProductDto>()
            .ForMember(d => d.CategoryName, opt => opt.MapFrom(s => s.Category.Name))
            .ForMember(d => d.UnitOfMeasure, opt => opt.MapFrom(s => s.UnitOfMeasure.ToString()))
            .ForMember(d => d.Images, opt => opt.MapFrom(s => s.Images))
            .ForMember(d => d.WorkspaceName, opt => opt.MapFrom(s => s.Workspace != null ? s.Workspace.Name : null));

        CreateMap<ProductImage, ProductImageDto>();

        CreateMap<Currency, CurrencyDto>();

        CreateMap<ExchangeRate, ExchangeRateDto>()
            .ForMember(d => d.CurrencyCode, opt => opt.MapFrom(s => s.Currency.Code));
    }
}
