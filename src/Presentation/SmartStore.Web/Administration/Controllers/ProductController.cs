﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Web.Mvc;
using Autofac;
using Newtonsoft.Json;
using SmartStore.Admin.Models.Catalog;
using SmartStore.Collections;
using SmartStore.Core;
using SmartStore.Core.Data;
using SmartStore.Core.Domain.Catalog;
using SmartStore.Core.Domain.Common;
using SmartStore.Core.Domain.Customers;
using SmartStore.Core.Domain.Directory;
using SmartStore.Core.Domain.Discounts;
using SmartStore.Core.Domain.Orders;
using SmartStore.Core.Domain.Seo;
using SmartStore.Core.Events;
using SmartStore.Core.Logging;
using SmartStore.Services;
using SmartStore.Services.Catalog;
using SmartStore.Services.Common;
using SmartStore.Services.Customers;
using SmartStore.Services.Directory;
using SmartStore.Services.Discounts;
using SmartStore.Services.Helpers;
using SmartStore.Services.Localization;
using SmartStore.Services.Media;
using SmartStore.Services.Orders;
using SmartStore.Services.Security;
using SmartStore.Services.Seo;
using SmartStore.Services.Stores;
using SmartStore.Services.Tax;
using SmartStore.Web.Framework;
using SmartStore.Web.Framework.Controllers;
using SmartStore.Web.Framework.Filters;
using SmartStore.Web.Framework.Modelling;
using SmartStore.Web.Framework.Security;
using Telerik.Web.Mvc;

namespace SmartStore.Admin.Controllers
{
	[AdminAuthorize]
    public partial class ProductController : AdminControllerBase
    {
        #region Fields

        private readonly IProductService _productService;
        private readonly IProductTemplateService _productTemplateService;
        private readonly ICategoryService _categoryService;
        private readonly IManufacturerService _manufacturerService;
        private readonly ICustomerService _customerService;
        private readonly IUrlRecordService _urlRecordService;
        private readonly IWorkContext _workContext;
        private readonly ILanguageService _languageService;
        private readonly ILocalizationService _localizationService;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly ISpecificationAttributeService _specificationAttributeService;
        private readonly IPictureService _pictureService;
        private readonly ITaxCategoryService _taxCategoryService;
        private readonly IProductTagService _productTagService;
        private readonly ICopyProductService _copyProductService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly IPermissionService _permissionService;
        private readonly IAclService _aclService;
		private readonly IStoreService _storeService;
		private readonly IStoreMappingService _storeMappingService;
        private readonly AdminAreaSettings _adminAreaSettings;
		private readonly IDateTimeHelper _dateTimeHelper;
		private readonly IDiscountService _discountService;
		private readonly IProductAttributeService _productAttributeService;
		private readonly IRepository<ProductVariantAttributeCombination> _pvacRepository;
		private readonly IBackInStockSubscriptionService _backInStockSubscriptionService;
		private readonly IShoppingCartService _shoppingCartService;
		private readonly IProductAttributeFormatter _productAttributeFormatter;
		private readonly IProductAttributeParser _productAttributeParser;
		private readonly CatalogSettings _catalogSettings;
		private readonly IDownloadService _downloadService;
		private readonly IDeliveryTimeService _deliveryTimesService;
        private readonly IQuantityUnitService _quantityUnitService;
		private readonly IMeasureService _measureService;
		private readonly MeasureSettings _measureSettings;
		private readonly IPriceFormatter _priceFormatter;
		private readonly IDbContext _dbContext;
		private readonly IEventPublisher _eventPublisher;
		private readonly IGenericAttributeService _genericAttributeService;
        private readonly ICommonServices _services;
		private readonly SeoSettings _seoSettings;

		#endregion

		#region Constructors

		public ProductController(
			IProductService productService, 
            IProductTemplateService productTemplateService,
            ICategoryService categoryService,
			IManufacturerService manufacturerService,
            ICustomerService customerService,
            IUrlRecordService urlRecordService,
			IWorkContext workContext,
			ILanguageService languageService, 
            ILocalizationService localizationService,
			ILocalizedEntityService localizedEntityService,
            ISpecificationAttributeService specificationAttributeService,
			IPictureService pictureService,
            ITaxCategoryService taxCategoryService,
			IProductTagService productTagService,
            ICopyProductService copyProductService,
            ICustomerActivityService customerActivityService,
            IPermissionService permissionService,
			IAclService aclService,
			IStoreService storeService,
			IStoreMappingService storeMappingService,
			AdminAreaSettings adminAreaSettings,
			IDateTimeHelper dateTimeHelper,
			IDiscountService discountService,
			IProductAttributeService productAttributeService,
			IRepository<ProductVariantAttributeCombination> pvacRepository,
            IBackInStockSubscriptionService backInStockSubscriptionService,
			IShoppingCartService shoppingCartService,
			IProductAttributeFormatter productAttributeFormatter,
			IProductAttributeParser productAttributeParser,
			CatalogSettings catalogSettings,
			IDownloadService downloadService,
			IDeliveryTimeService deliveryTimesService,
            IQuantityUnitService quantityUnitService,
			IMeasureService measureService,
			MeasureSettings measureSettings,
			IPriceFormatter priceFormatter,
			IDbContext dbContext,
			IEventPublisher eventPublisher,
			IGenericAttributeService genericAttributeService,
            ICommonServices services,
			SeoSettings seoSettings)
		{
            this._productService = productService;
            this._productTemplateService = productTemplateService;
            this._categoryService = categoryService;
            this._manufacturerService = manufacturerService;
            this._customerService = customerService;
            this._urlRecordService = urlRecordService;
            this._workContext = workContext;
            this._languageService = languageService;
            this._localizationService = localizationService;
            this._localizedEntityService = localizedEntityService;
            this._specificationAttributeService = specificationAttributeService;
            this._pictureService = pictureService;
            this._taxCategoryService = taxCategoryService;
            this._productTagService = productTagService;
            this._copyProductService = copyProductService;
            this._customerActivityService = customerActivityService;
            this._permissionService = permissionService;
            this._aclService = aclService;
			this._storeService = storeService;
			this._storeMappingService = storeMappingService;
            this._adminAreaSettings = adminAreaSettings;
			this._dateTimeHelper = dateTimeHelper;
			this._discountService = discountService;
			this._productAttributeService = productAttributeService;
			this._pvacRepository = pvacRepository;
			this._backInStockSubscriptionService = backInStockSubscriptionService;
			this._shoppingCartService = shoppingCartService;
			this._productAttributeFormatter = productAttributeFormatter;
			this._productAttributeParser = productAttributeParser;
			this._catalogSettings = catalogSettings;
			this._downloadService = downloadService;
			this._deliveryTimesService = deliveryTimesService;
            this._quantityUnitService = quantityUnitService;
			this._measureService = measureService;
			this._measureSettings = measureSettings;
			this._priceFormatter = priceFormatter;
			this._dbContext = dbContext;
			this._eventPublisher = eventPublisher;
			this._genericAttributeService = genericAttributeService;
            _services = services;
			_seoSettings = seoSettings;
		}

        #endregion

		#region Update[...]

		[NonAction]
		protected void UpdateProductGeneralInfo(Product product, ProductModel model)
		{
			var p = product;
			var m = model;

			p.ProductTypeId = m.ProductTypeId;
			p.VisibleIndividually = m.VisibleIndividually;
			p.ProductTemplateId = m.ProductTemplateId;

			p.Name = m.Name;
			p.ShortDescription = m.ShortDescription;
			p.FullDescription = m.FullDescription;
			p.Sku = m.Sku;
			p.ManufacturerPartNumber = m.ManufacturerPartNumber;
			p.Gtin = m.Gtin;
			p.AdminComment = m.AdminComment;
			p.AvailableStartDateTimeUtc = m.AvailableStartDateTimeUtc;
			p.AvailableEndDateTimeUtc = m.AvailableEndDateTimeUtc;

			p.AllowCustomerReviews = m.AllowCustomerReviews;
			p.ShowOnHomePage = m.ShowOnHomePage;
			p.HomePageDisplayOrder = m.HomePageDisplayOrder;
			p.Published = m.Published;
			p.RequireOtherProducts = m.RequireOtherProducts;
			p.RequiredProductIds = m.RequiredProductIds;
			p.AutomaticallyAddRequiredProducts = m.AutomaticallyAddRequiredProducts;

			p.IsGiftCard = m.IsGiftCard;
			p.GiftCardTypeId = m.GiftCardTypeId;
			
			p.IsDownload = m.IsDownload;
			p.DownloadId = m.DownloadId;
			p.UnlimitedDownloads = m.UnlimitedDownloads;
			p.MaxNumberOfDownloads = m.MaxNumberOfDownloads;
			p.DownloadExpirationDays = m.DownloadExpirationDays;
			p.DownloadActivationTypeId = m.DownloadActivationTypeId;
			p.HasUserAgreement = m.HasUserAgreement;
			p.UserAgreementText = m.UserAgreementText;
			p.HasSampleDownload = m.HasSampleDownload;
			p.SampleDownloadId = m.SampleDownloadId == 0 ? (int?)null : m.SampleDownloadId;

			p.IsRecurring = m.IsRecurring;
			p.RecurringCycleLength = m.RecurringCycleLength;
			p.RecurringCyclePeriodId = m.RecurringCyclePeriodId;
			p.RecurringTotalCycles = m.RecurringTotalCycles;

			p.IsShipEnabled = m.IsShipEnabled;
			p.DeliveryTimeId = m.DeliveryTimeId == 0 ? (int?)null : m.DeliveryTimeId;
            p.QuantityUnitId = m.QuantityUnitId == 0 ? (int?)null : m.QuantityUnitId;
			p.IsFreeShipping = m.IsFreeShipping;
			p.AdditionalShippingCharge = m.AdditionalShippingCharge;
			p.Weight = m.Weight;
			p.Length = m.Length;
			p.Width = m.Width;
			p.Height = m.Height;

			p.IsEsd = m.IsEsd;
			p.IsTaxExempt = m.IsTaxExempt;
			p.TaxCategoryId = m.TaxCategoryId;

			p.UpdatedOnUtc = DateTime.UtcNow;
			p.AvailableEndDateTimeUtc = p.AvailableEndDateTimeUtc.ToEndOfTheDay();
			p.SpecialPriceEndDateTimeUtc = p.SpecialPriceEndDateTimeUtc.ToEndOfTheDay();
		}

		[NonAction]
		protected void UpdateProductTags(Product product, string rawProductTags)
		{
			if (product == null)
				throw new ArgumentNullException("product");

			var productTags = new List<string>();

			foreach (string str in rawProductTags.SplitSafe(","))
			{
				string tag = str.TrimSafe();
				if (tag.HasValue())
					productTags.Add(tag);
			}

			var existingProductTags = product.ProductTags.ToList();
			var productTagsToRemove = new List<ProductTag>();

			foreach (var existingProductTag in existingProductTags)
			{
				bool found = false;
				foreach (string newProductTag in productTags)
				{
					if (existingProductTag.Name.Equals(newProductTag, StringComparison.InvariantCultureIgnoreCase))
					{
						found = true;
						break;
					}
				}
				if (!found)
				{
					productTagsToRemove.Add(existingProductTag);
				}
			}

			foreach (var productTag in productTagsToRemove)
			{
				product.ProductTags.Remove(productTag);
				_productService.UpdateProduct(product);
			}

			foreach (string productTagName in productTags)
			{
				ProductTag productTag = null;
				var productTag2 = _productTagService.GetProductTagByName(productTagName);

				if (productTag2 == null)
				{
					//add new product tag
					productTag = new ProductTag()
					{
						Name = productTagName
					};
					_productTagService.InsertProductTag(productTag);
				}
				else
				{
					productTag = productTag2;
				}

				if (!product.ProductTagExists(productTag.Id))
				{
					product.ProductTags.Add(productTag);
					_productService.UpdateProduct(product);
				}
			}
		}

		[NonAction]
		protected void UpdateProductInventory(Product product, ProductModel model)
		{
			var p = product;
			var m = model;

			var prevStockQuantity = product.StockQuantity;

			p.ManageInventoryMethodId = m.ManageInventoryMethodId;
			p.StockQuantity = m.StockQuantity;
			p.DisplayStockAvailability = m.DisplayStockAvailability;
			p.DisplayStockQuantity = m.DisplayStockQuantity;
			p.MinStockQuantity = m.MinStockQuantity;
			p.LowStockActivityId = m.LowStockActivityId;
			p.NotifyAdminForQuantityBelow = m.NotifyAdminForQuantityBelow;
			p.BackorderModeId = m.BackorderModeId;
			p.AllowBackInStockSubscriptions = m.AllowBackInStockSubscriptions;
			p.OrderMinimumQuantity = m.OrderMinimumQuantity;
			p.OrderMaximumQuantity = m.OrderMaximumQuantity;

			// back in stock notifications
			if (p.ManageInventoryMethod == ManageInventoryMethod.ManageStock &&
				p.BackorderMode == BackorderMode.NoBackorders &&
				p.AllowBackInStockSubscriptions &&
				p.StockQuantity > 0 &&
				prevStockQuantity <= 0 &&
				p.Published &&
				!p.Deleted)
			{
				_backInStockSubscriptionService.SendNotificationsToSubscribers(p);
			}

			if (p.StockQuantity != prevStockQuantity && p.ManageInventoryMethod == ManageInventoryMethod.ManageStock)
			{
				_productService.AdjustInventory(p, true, 0, string.Empty);
			}
		}

		[NonAction]
		protected void UpdateProductBundleItems(Product product, ProductModel model)
		{
			var p = product;
			var m = model;

			p.BundleTitleText = m.BundleTitleText;
			p.BundlePerItemPricing = m.BundlePerItemPricing;
			p.BundlePerItemShipping = m.BundlePerItemShipping;
			p.BundlePerItemShoppingCart = m.BundlePerItemShoppingCart;

			// SEO
			foreach (var localized in model.Locales)
			{
				_localizedEntityService.SaveLocalizedValue(p, x => x.BundleTitleText, localized.BundleTitleText, localized.LanguageId);
			}
		}

		[NonAction]
		protected void UpdateProductPrice(Product product, ProductModel model)
		{
			var p = product;
			var m = model;

			p.Price = m.Price;
			p.OldPrice = m.OldPrice;
			p.ProductCost = m.ProductCost;
			p.SpecialPrice = m.SpecialPrice;
			p.SpecialPriceStartDateTimeUtc = m.SpecialPriceStartDateTimeUtc;
			p.SpecialPriceEndDateTimeUtc = m.SpecialPriceEndDateTimeUtc;
			p.DisableBuyButton = m.DisableBuyButton;
			p.DisableWishlistButton = m.DisableWishlistButton;
			p.AvailableForPreOrder = m.AvailableForPreOrder;
			p.CallForPrice = m.CallForPrice;
			p.CustomerEntersPrice = m.CustomerEntersPrice;
			p.MinimumCustomerEnteredPrice = m.MinimumCustomerEnteredPrice;
			p.MaximumCustomerEnteredPrice = m.MaximumCustomerEnteredPrice;

			p.BasePriceEnabled = m.BasePriceEnabled;
			p.BasePriceBaseAmount = m.BasePriceBaseAmount;
			p.BasePriceAmount = m.BasePriceAmount;
            p.BasePriceMeasureUnit = m.BasePriceMeasureUnit;
		}

		[NonAction]
		protected void UpdateProductSeo(Product product, ProductModel model)
		{
			var p = product;
			var m = model;

			p.MetaKeywords = m.MetaKeywords;
			p.MetaDescription = m.MetaDescription;
			p.MetaTitle = m.MetaTitle;

			// SEO
			var service = _localizedEntityService;
			foreach (var localized in model.Locales)
			{
				service.SaveLocalizedValue(p, x => x.MetaKeywords, localized.MetaKeywords, localized.LanguageId);
				service.SaveLocalizedValue(p, x => x.MetaDescription, localized.MetaDescription, localized.LanguageId);
				service.SaveLocalizedValue(p, x => x.MetaTitle, localized.MetaTitle, localized.LanguageId);
			}
		}

		[NonAction]
		protected void UpdateProductDiscounts(Product product, ProductModel model)
		{
			var allDiscounts = _discountService.GetAllDiscounts(DiscountType.AssignedToSkus, null, true);
			foreach (var discount in allDiscounts)
			{
				if (model.SelectedDiscountIds != null && model.SelectedDiscountIds.Contains(discount.Id))
				{
					//new role
					if (product.AppliedDiscounts.Count(d => d.Id == discount.Id) == 0)
						product.AppliedDiscounts.Add(discount);
				}
				else
				{
					//removed role
					if (product.AppliedDiscounts.Count(d => d.Id == discount.Id) > 0)
						product.AppliedDiscounts.Remove(discount);
				}
			}
			_productService.UpdateProduct(product);
			_productService.UpdateHasDiscountsApplied(product);
		}

		[NonAction]
		protected void UpdateProductAcl(Product product, ProductModel model)
		{
			product.SubjectToAcl = model.SubjectToAcl;

			var existingAclRecords = _aclService.GetAclRecords(product);
			var allCustomerRoles = _customerService.GetAllCustomerRoles(true);
			foreach (var customerRole in allCustomerRoles)
			{
				if (model.SelectedCustomerRoleIds != null && model.SelectedCustomerRoleIds.Contains(customerRole.Id))
				{
					//new role
					if (existingAclRecords.Where(acl => acl.CustomerRoleId == customerRole.Id).Count() == 0)
						_aclService.InsertAclRecord(product, customerRole.Id);
				}
				else
				{
					//removed role
					var aclRecordToDelete = existingAclRecords.Where(acl => acl.CustomerRoleId == customerRole.Id).FirstOrDefault();
					if (aclRecordToDelete != null)
						_aclService.DeleteAclRecord(aclRecordToDelete);
				}
			}
		}

		[NonAction]
		protected void UpdateStoreMappings(Product product, ProductModel model)
		{
			product.LimitedToStores = model.LimitedToStores;

			var existingStoreMappings = _storeMappingService.GetStoreMappings(product);
			var allStores = _storeService.GetAllStores();
			foreach (var store in allStores)
			{
				if (model.SelectedStoreIds != null && model.SelectedStoreIds.Contains(store.Id))
				{
					if (existingStoreMappings.Where(sm => sm.StoreId == store.Id).Count() == 0)
					{
						_storeMappingService.InsertStoreMapping(product, store.Id);
					}
				}
				else
				{
					var storeMappingToDelete = existingStoreMappings.Where(sm => sm.StoreId == store.Id).FirstOrDefault();
					if (storeMappingToDelete != null)
					{
						_storeMappingService.DeleteStoreMapping(storeMappingToDelete);
					}
				}
			}
		}

		[NonAction]
		private void UpdateLocales(Product product, ProductModel model, bool general, bool seo, bool bundles)
		{
			// TODO: Obsolete
		}

		[NonAction]
		private void UpdateLocales(ProductTag productTag, ProductTagModel model)
		{
			foreach (var localized in model.Locales)
			{
				_localizedEntityService.SaveLocalizedValue(productTag, x => x.Name, localized.Name, localized.LanguageId);
			}
		}

		[NonAction]
		private void UpdateLocales(ProductVariantAttributeValue pvav, ProductModel.ProductVariantAttributeValueModel model)
		{
			foreach (var localized in model.Locales)
			{
				_localizedEntityService.SaveLocalizedValue(pvav, x => x.Name, localized.Name, localized.LanguageId);
			}
		}

		[NonAction]
		private void UpdatePictureSeoNames(Product product)
		{
			foreach (var pp in product.ProductPictures)
			{
				_pictureService.SetSeoFilename(pp.PictureId, _pictureService.GetPictureSeName(product.Name));
			}
		}

		[NonAction]
		private void UpdateDataOfExistingProduct(Product product, ProductModel model, bool editMode)
		{
			var p = product;
			var m = model;

			var modifiedProperties = editMode ? _dbContext.GetModifiedProperties(p): new Dictionary<string, object>();

			var nameChanged = modifiedProperties.ContainsKey("Name");
			var seoTabLoaded = m.LoadedTabs.Contains("SEO", StringComparer.OrdinalIgnoreCase);

			// Handle Download transiency
			MediaHelper.UpdateDownloadTransientStateFor(p, x => x.DownloadId);
			MediaHelper.UpdateDownloadTransientStateFor(p, x => x.SampleDownloadId);

			// SEO
			m.SeName = p.ValidateSeName(m.SeName, p.Name, true, _urlRecordService, _seoSettings);
			_urlRecordService.SaveSlug(p, m.SeName, 0);

			foreach (var localized in model.Locales)
			{
				_localizedEntityService.SaveLocalizedValue(product, x => x.Name, localized.Name, localized.LanguageId);
				_localizedEntityService.SaveLocalizedValue(product, x => x.ShortDescription, localized.ShortDescription, localized.LanguageId);
				_localizedEntityService.SaveLocalizedValue(product, x => x.FullDescription, localized.FullDescription, localized.LanguageId);

				// search engine name
				var localizedSeName = p.ValidateSeName(localized.SeName, localized.Name, false, _urlRecordService, _seoSettings, localized.LanguageId);
				_urlRecordService.SaveSlug(p, localizedSeName, localized.LanguageId);
			}

			// picture seo names
			if (nameChanged)
			{
				UpdatePictureSeoNames(p);
			}
			
			// product tags
			UpdateProductTags(p, m.ProductTags);
		}

		#endregion

		#region Utitilies

        [NonAction]
        private void PrepareAclModel(ProductModel model, Product product, bool excludeProperties)
        {
            if (model == null)
                throw new ArgumentNullException("model");

            model.AvailableCustomerRoles = _customerService
                .GetAllCustomerRoles(true)
                .Select(cr => cr.ToModel())
                .ToList();
            if (!excludeProperties)
            {
                if (product != null)
                {
                    model.SelectedCustomerRoleIds = _aclService.GetCustomerRoleIdsWithAccess(product);
                }
                else
                {
                    model.SelectedCustomerRoleIds = new int[0];
                }
            }
        }

		[NonAction]
		private void PrepareStoresMappingModel(ProductModel model, Product product, bool excludeProperties)
		{
			if (model == null)
				throw new ArgumentNullException("model");

			model.AvailableStores = _storeService
				.GetAllStores()
				.Select(s => s.ToModel())
				.ToList();
			if (!excludeProperties)
			{
				if (product != null)
				{
					model.SelectedStoreIds = _storeMappingService.GetStoresIdsWithAccess(product);
				}
				else
				{
					model.SelectedStoreIds = new int[0];
				}
			}
		}

		[NonAction]
		protected void PrepareProductModel(ProductModel model, Product product, bool setPredefinedValues, bool excludeProperties)
		{
			if (model == null)
				throw new ArgumentNullException("model");

			if (product != null)
			{
				var parentGroupedProduct = _productService.GetProductById(product.ParentGroupedProductId);
				if (parentGroupedProduct != null)
				{
					model.AssociatedToProductId = product.ParentGroupedProductId;
					model.AssociatedToProductName = parentGroupedProduct.Name;
				}

				model.CreatedOn = _dateTimeHelper.ConvertToUserTime(product.CreatedOnUtc, DateTimeKind.Utc);
				model.UpdatedOn = _dateTimeHelper.ConvertToUserTime(product.UpdatedOnUtc, DateTimeKind.Utc);

				if (product.LimitedToStores)
				{
					var storeMappings = _storeMappingService.GetStoreMappings(product);
					if (storeMappings.FirstOrDefault(x => x.StoreId == _services.StoreContext.CurrentStore.Id) == null)
					{
						var storeMapping = storeMappings.FirstOrDefault();
						if (storeMapping != null)
						{
							var store = _services.StoreService.GetStoreById(storeMapping.StoreId);
							if (store != null)
								model.ProductUrl = store.Url.EnsureEndsWith("/") + product.GetSeName(); 
						}
					}
				}

				if (model.ProductUrl.IsEmpty())
				{
					model.ProductUrl = Url.RouteUrl("Product", new { SeName = product.GetSeName() }, Request.Url.Scheme);
				}
			}

			model.PrimaryStoreCurrencyCode = _services.StoreContext.CurrentStore.PrimaryStoreCurrency.CurrencyCode;
			model.BaseWeightIn = _measureService.GetMeasureWeightById(_measureSettings.BaseWeightId).Name;
			model.BaseDimensionIn = _measureService.GetMeasureDimensionById(_measureSettings.BaseDimensionId).Name;

			model.NumberOfAvailableProductAttributes = _productAttributeService.GetAllProductAttributes().Count;
			model.NumberOfAvailableManufacturers = _manufacturerService.GetAllManufacturers("",	pageIndex: 0, pageSize: 1, showHidden: true).TotalCount;
			model.NumberOfAvailableCategories = _categoryService.GetAllCategories(pageIndex: 0, pageSize: 1, showHidden: true).TotalCount;

			//copy product
			if (product != null)
			{
				model.CopyProductModel.Id = product.Id;
				model.CopyProductModel.Name = "{0} {1}".FormatInvariant(T("Admin.Common.CopyOf"), product.Name);
				model.CopyProductModel.Published = true;
				model.CopyProductModel.CopyImages = true;
			}

			//templates
			var templates = _productTemplateService.GetAllProductTemplates();
			foreach (var template in templates)
			{
				model.AvailableProductTemplates.Add(new SelectListItem
				{
					Text = template.Name,
					Value = template.Id.ToString()
				});
			}

			//product tags
			var allTags = _productTagService.GetAllProductTagNames();
			foreach (var tag in allTags)
			{
				model.AvailableProductTags.Add(new SelectListItem { Text = tag, Value = tag });
			}

			if (product != null)
			{
				//var tags = product.ProductTags;
				model.ProductTags = string.Join(", ", product.ProductTags.Select(x => x.Name));
			}

			//tax categories
			var taxCategories = _taxCategoryService.GetAllTaxCategories();
			foreach (var tc in taxCategories)
			{
				model.AvailableTaxCategories.Add(new SelectListItem
				{
					Text = tc.Name,
					Value = tc.Id.ToString(),
					Selected = product != null && !setPredefinedValues && tc.Id == product.TaxCategoryId
				});
			}

			// delivery times
			var deliveryTimes = _deliveryTimesService.GetAllDeliveryTimes();
			foreach (var dt in deliveryTimes)
			{
				model.AvailableDeliveryTimes.Add(new SelectListItem
				{
					Text = dt.Name,
					Value = dt.Id.ToString(),
					Selected = product != null && !setPredefinedValues && dt.Id == product.DeliveryTimeId.GetValueOrDefault()
				});
			}

            // quantity units
            var quantityUnits = _quantityUnitService.GetAllQuantityUnits();
            foreach (var mu in quantityUnits)
            {
                model.AvailableQuantityUnits.Add(new SelectListItem
                {
                    Text = mu.Name,
                    Value = mu.Id.ToString(),
                    Selected = product != null && !setPredefinedValues && mu.Id == product.QuantityUnitId.GetValueOrDefault()
                });
            }

			// BasePrice aka PAnGV
			var measureUnits = _measureService.GetAllMeasureWeights()
				.Select(x => x.SystemKeyword).Concat(_measureService.GetAllMeasureDimensions().Select(x => x.SystemKeyword)).ToList();

			// don't forget biz import!
            if (product != null && !setPredefinedValues && product.BasePriceMeasureUnit.HasValue() && !measureUnits.Exists(u => u.IsCaseInsensitiveEqual(product.BasePriceMeasureUnit)))
			{
                measureUnits.Add(product.BasePriceMeasureUnit);
			}

            foreach (var mu in measureUnits)
			{
				model.AvailableMeasureUnits.Add(new SelectListItem
				{
					Text = mu,
					Value = mu,
					Selected = product != null && !setPredefinedValues && mu.Equals(product.BasePriceMeasureUnit, StringComparison.OrdinalIgnoreCase)
				});
			}

			//specification attributes
			var specificationAttributes = _specificationAttributeService.GetSpecificationAttributes().ToList();
			for (int i = 0; i < specificationAttributes.Count; i++)
			{
				var sa = specificationAttributes[i];
				model.AddSpecificationAttributeModel.AvailableAttributes.Add(new SelectListItem { Text = sa.Name, Value = sa.Id.ToString() });
				if (i == 0)
				{
					//attribute options
					foreach (var sao in _specificationAttributeService.GetSpecificationAttributeOptionsBySpecificationAttribute(sa.Id))
					{
						model.AddSpecificationAttributeModel.AvailableOptions.Add(new SelectListItem { Text = sao.Name, Value = sao.Id.ToString() });
					}
				}
			}
			//default specs values
			model.AddSpecificationAttributeModel.ShowOnProductPage = true;

			//discounts
			var discounts = _discountService.GetAllDiscounts(DiscountType.AssignedToSkus, null, true);
			model.AvailableDiscounts = discounts.ToList();
			if (product != null && !excludeProperties)
			{
				model.SelectedDiscountIds = product.AppliedDiscounts.Select(d => d.Id).ToArray();
			}

			var inventoryMethods = ((ManageInventoryMethod[])Enum.GetValues(typeof(ManageInventoryMethod))).Where(
				x => (model.ProductTypeId == (int)ProductType.BundledProduct && x == ManageInventoryMethod.ManageStockByAttributes) ? false : true
			);

			foreach (var inventoryMethod in inventoryMethods)
			{
				model.AvailableManageInventoryMethods.Add(new SelectListItem
				{
					Value = ((int)inventoryMethod).ToString(),
					Text = inventoryMethod.GetLocalizedEnum(_localizationService, _workContext),
					Selected = ((int)inventoryMethod == model.ManageInventoryMethodId)
				});
			}

			if (setPredefinedValues)
			{
				model.MaximumCustomerEnteredPrice = 1000;
				model.MaxNumberOfDownloads = 10;
				model.RecurringCycleLength = 100;
				model.RecurringTotalCycles = 10;
				model.StockQuantity = 10000;
				model.NotifyAdminForQuantityBelow = 1;
				model.OrderMinimumQuantity = 1;
				model.OrderMaximumQuantity = 10000;

				model.UnlimitedDownloads = true;
				model.IsShipEnabled = true;
				model.AllowCustomerReviews = true;
				model.Published = true;
				model.VisibleIndividually = true;
			}
		}

        [NonAction]
        private void PrepareProductPictureThumbnailModel(ProductModel model, Product product)
        {
            if (model == null)
                throw new ArgumentNullException("model");

            if (product != null && _adminAreaSettings.DisplayProductPictures)
            {
                var defaultProductPicture = _pictureService.GetPicturesByProductId(product.Id, 1).FirstOrDefault();
                model.PictureThumbnailUrl = _pictureService.GetPictureUrl(defaultProductPicture, 75, true);
                model.NoThumb = defaultProductPicture == null;
            }
        }

		#endregion Utitilies

		#region Methods

		#region Misc

		[HttpPost]
		public ActionResult GetBasePrice(int productId, string basePriceMeasureUnit, decimal basePriceAmount, int basePriceBaseAmount)
		{
			var product = _productService.GetProductById(productId);
			string basePrice = "";

			if (basePriceAmount != Decimal.Zero)
			{
				decimal basePriceValue = Convert.ToDecimal((product.Price / basePriceAmount) * basePriceBaseAmount);

				string basePriceFormatted = _priceFormatter.FormatPrice(basePriceValue, true, false);
				string unit = "{0} {1}".FormatWith(basePriceBaseAmount, basePriceMeasureUnit);

				basePrice = _localizationService.GetResource("Admin.Catalog.Products.Fields.BasePriceInfo").FormatWith(
					basePriceAmount.ToString("G29") + " " + basePriceMeasureUnit, basePriceFormatted, unit);
			}

			return Json(new { Result = true, BasePrice = basePrice });
		}

		#endregion

		#region Product list / create / edit / delete

		//list products
        public ActionResult Index()
        {
            return RedirectToAction("List");
        }

        public ActionResult List(ProductListModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
                return AccessDeniedView();

			var allStores = _storeService.GetAllStores();

            model.DisplayProductPictures = _adminAreaSettings.DisplayProductPictures;
			model.GridPageSize = _adminAreaSettings.GridPageSize;

            var allCategories = _categoryService.GetAllCategories(showHidden: true);
            var mappedCategories = allCategories.ToDictionary(x => x.Id);
            foreach (var c in allCategories)
            {
                model.AvailableCategories.Add(new SelectListItem { Text = c.GetCategoryNameWithPrefix(_categoryService, mappedCategories), Value = c.Id.ToString() });
            }

            foreach (var m in _manufacturerService.GetAllManufacturers(true))
            {
                model.AvailableManufacturers.Add(new SelectListItem { Text = m.Name, Value = m.Id.ToString() });
            }

            foreach (var s in allStores)
            {
                model.AvailableStores.Add(new SelectListItem { Text = s.Name, Value = s.Id.ToString() });
            }

			model.AvailableProductTypes = ProductType.SimpleProduct.ToSelectList(false).ToList();

            return View(model);
        }

		[HttpPost, GridAction(EnableCustomBinding = true)]
		public ActionResult ProductList(GridCommand command, ProductListModel model)
		{
			var gridModel = new GridModel<ProductModel>();

			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				var searchContext = new ProductSearchContext
				{
					ManufacturerId = model.SearchManufacturerId,
					StoreId = model.SearchStoreId,
					Keywords = model.SearchProductName,
					SearchSku = !_catalogSettings.SuppressSkuSearch,
					LanguageId = _workContext.WorkingLanguage.Id,
					OrderBy = ProductSortingEnum.Position,
					PageIndex = command.Page - 1,
					PageSize = command.PageSize,
					ShowHidden = true,
					ProductType = (model.SearchProductTypeId > 0 ? (ProductType?)model.SearchProductTypeId : null),
					WithoutCategories = model.SearchWithoutCategories,
					WithoutManufacturers = model.SearchWithoutManufacturers,
					IsPublished = model.SearchIsPublished,
					HomePageProducts = model.SearchHomePageProducts
				};

				if (model.SearchCategoryId > 0)
					searchContext.CategoryIds.Add(model.SearchCategoryId);

				if (command.SortDescriptors != null && command.SortDescriptors.Count > 0)
				{
					var sort = command.SortDescriptors.First();
					if (sort.Member == "Name")
					{
						searchContext.OrderBy = (sort.SortDirection == ListSortDirection.Ascending ? ProductSortingEnum.NameAsc : ProductSortingEnum.NameDesc);
					}
					else if (sort.Member == "Price")
					{
						searchContext.OrderBy = (sort.SortDirection == ListSortDirection.Ascending ? ProductSortingEnum.PriceAsc : ProductSortingEnum.PriceDesc);
					}
					else if (sort.Member == "CreatedOn")
					{
						searchContext.OrderBy = (sort.SortDirection == ListSortDirection.Ascending ? ProductSortingEnum.CreatedOnAsc : ProductSortingEnum.CreatedOn);
					}
				}

				var products = _productService.SearchProducts(searchContext);

				gridModel.Data = products.Select(x =>
				{
                    var productModel = new ProductModel
                    {
                        Sku = x.Sku,
                        Published = x.Published,
                        ProductTypeLabelHint = x.ProductTypeLabelHint,
                        Name = x.Name,
                        Id = x.Id,
                        StockQuantity = x.StockQuantity,
                        Price = x.Price,
                        LimitedToStores = x.LimitedToStores
                    };

					PrepareProductPictureThumbnailModel(productModel, x);

					productModel.ProductTypeName = x.GetProductTypeLabel(_localizationService);
					productModel.UpdatedOn = _dateTimeHelper.ConvertToUserTime(x.UpdatedOnUtc, DateTimeKind.Utc);
					productModel.CreatedOn = _dateTimeHelper.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc);

					return productModel;
				});

				gridModel.Total = products.TotalCount;
			}
			else
			{
				gridModel.Data = Enumerable.Empty<ProductModel>();

				NotifyAccessDenied();
			}

			return new JsonResult
			{
				Data = gridModel
			};
        }

        [HttpPost, ActionName("List")]
        [FormValueRequired("go-to-product-by-sku")]
        public ActionResult GoToSku(ProductListModel model)
        {
            string sku = model.GoDirectlyToSku;

			if (sku.HasValue())
			{
				var product = _productService.GetProductBySku(sku);
				if (product != null)
					return RedirectToAction("Edit", "Product", new { id = product.Id });

				var combination = _productAttributeService.GetProductVariantAttributeCombinationBySku(sku);
				if (combination != null && combination.Product != null && !combination.Product.Deleted)
					return RedirectToAction("Edit", "Product", new { id = combination.Product.Id });
			}

            //not found
            return List(model);
        }

        //create product
        public ActionResult Create()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
                return AccessDeniedView();

            var model = new ProductModel();
			PrepareProductModel(model, null, true, true);

            //product
            AddLocales(_languageService, model.Locales);
            PrepareAclModel(model, null, false);
			PrepareStoresMappingModel(model, null, false);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
		[ValidateInput(false)]
        public ActionResult Create(ProductModel model, bool continueEditing, FormCollection form)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
                return AccessDeniedView();

            if (ModelState.IsValid)
            {
				var product = new Product();

				MapModelToProduct(model, product, form);

				product.CreatedOnUtc = DateTime.UtcNow;
				product.StockQuantity = 10000;
				product.OrderMinimumQuantity = 1;
				product.OrderMaximumQuantity = 10000;
				product.IsShipEnabled = true;
				product.AllowCustomerReviews = true;
				product.Published = true;
				product.VisibleIndividually = true;
				product.MaximumCustomerEnteredPrice = 1000;

				if (product.ProductType == ProductType.BundledProduct)
				{
					product.BundleTitleText = _localizationService.GetResource("Products.Bundle.BundleIncludes");
				}

                _productService.InsertProduct(product);

				UpdateDataOfExistingProduct(product, model, false);

                //activity log
                _customerActivityService.InsertActivity("AddNewProduct", _localizationService.GetResource("ActivityLog.AddNewProduct"), product.Name);

                NotifySuccess(_localizationService.GetResource("Admin.Catalog.Products.Added"));

				if (continueEditing)
				{
					// ensure that the same tab gets selected in edit view
					var selectedTab = TempData["SelectedTab.product-edit"] as SmartStore.Web.Framework.UI.SelectedTabInfo;
					if (selectedTab != null)
					{
						selectedTab.Path = Url.Action("Edit", new System.Web.Routing.RouteValueDictionary { {"id", product.Id} });
					}
				}

                return continueEditing ? RedirectToAction("Edit", new { id = product.Id }) : RedirectToAction("List");
            }

            //If we got this far, something failed, redisplay form
			PrepareProductModel(model, null, false, true);
            PrepareAclModel(model, null, true);
			PrepareStoresMappingModel(model, null, true);
            return View(model);
        }

        //edit product
        public ActionResult Edit(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
                return AccessDeniedView();

            var product = _productService.GetProductById(id);
            
            if (product == null || product.Deleted)
                return RedirectToAction("List");

            var model = product.ToModel();
			PrepareProductModel(model, product, false, false);

            AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.Name = product.GetLocalized(x => x.Name, languageId, false, false);
                locale.ShortDescription = product.GetLocalized(x => x.ShortDescription, languageId, false, false);
                locale.FullDescription = product.GetLocalized(x => x.FullDescription, languageId, false, false);
                locale.MetaKeywords = product.GetLocalized(x => x.MetaKeywords, languageId, false, false);
                locale.MetaDescription = product.GetLocalized(x => x.MetaDescription, languageId, false, false);
                locale.MetaTitle = product.GetLocalized(x => x.MetaTitle, languageId, false, false);
                locale.SeName = product.GetSeName(languageId, false, false);
				locale.BundleTitleText = product.GetLocalized(x => x.BundleTitleText, languageId, false, false);
            });

            PrepareProductPictureThumbnailModel(model, product);
            PrepareAclModel(model, product, false);
			PrepareStoresMappingModel(model, product, false);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
		[ValidateInput(false)]
        public ActionResult Edit(ProductModel model, bool continueEditing, FormCollection form)
        {
			if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				return AccessDeniedView();
			}

            var product = _productService.GetProductById(model.Id);
			if (product == null || product.Deleted)
			{
				return RedirectToAction("List");
			}

            if (ModelState.IsValid)
            {
				MapModelToProduct(model, product, form);
				UpdateDataOfExistingProduct(product, model, true);

                // activity log
                _customerActivityService.InsertActivity("EditProduct", _localizationService.GetResource("ActivityLog.EditProduct"), product.Name);

                NotifySuccess(_localizationService.GetResource("Admin.Catalog.Products.Updated"));
                return continueEditing ? RedirectToAction("Edit", new { id = product.Id }) : RedirectToAction("List");
            }

            //If we got this far, something failed, redisplay form
			PrepareProductModel(model, product, false, true);
            PrepareProductPictureThumbnailModel(model, product);
            PrepareAclModel(model, product, true);
			PrepareStoresMappingModel(model, product, true);

            return View(model);
        }

		[NonAction]
		protected void MapModelToProduct(ProductModel model, Product product, FormCollection form)
		{
			if (model.LoadedTabs == null || model.LoadedTabs.Length == 0)
			{
				model.LoadedTabs = new string[] { "Info" };
			}

			foreach (var tab in model.LoadedTabs)
			{
                switch (tab.ToLowerInvariant())
				{
					case "info":
						UpdateProductGeneralInfo(product, model);
						break;
					case "inventory":
						UpdateProductInventory(product, model);
						break;
					case "bundleitems":
						UpdateProductBundleItems(product, model);
						break;
					case "price":
						UpdateProductPrice(product, model);
						break;
					case "discounts":
						UpdateProductDiscounts(product, model);
						break;
					case "seo":
						UpdateProductSeo(product, model);
						break;
					case "acl":
						UpdateProductAcl(product, model);
						break;
					case "stores":
						UpdateStoreMappings(product, model);
						break;
				}
			}

			_eventPublisher.Publish(new ModelBoundEvent(model, product, form));
		}

		public ActionResult LoadEditTab(int id, string tabName, string viewPath = null)
		{
			if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
				return Content("Error while loading template: Access denied.");
			
			try
			{
				if (id == 0)
				{
					// is Create mode
					return PartialView("_Create.SaveFirst");
				}

				if (tabName.IsEmpty())
				{
					return Content("A unique tab name has to specified (route parameter: tabName)");
				}
				
				var product = _productService.GetProductById(id);
				
				var model = product.ToModel();
				
				PrepareProductModel(model, product, false, false);

				AddLocales(_languageService, model.Locales, (locale, languageId) =>
				{
					locale.Name = product.GetLocalized(x => x.Name, languageId, false, false);
					locale.ShortDescription = product.GetLocalized(x => x.ShortDescription, languageId, false, false);
					locale.FullDescription = product.GetLocalized(x => x.FullDescription, languageId, false, false);
					locale.MetaKeywords = product.GetLocalized(x => x.MetaKeywords, languageId, false, false);
					locale.MetaDescription = product.GetLocalized(x => x.MetaDescription, languageId, false, false);
					locale.MetaTitle = product.GetLocalized(x => x.MetaTitle, languageId, false, false);
					locale.SeName = product.GetSeName(languageId, false, false);
					locale.BundleTitleText = product.GetLocalized(x => x.BundleTitleText, languageId, false, false);
				});

				PrepareProductPictureThumbnailModel(model, product);
				PrepareAclModel(model, product, false);
				PrepareStoresMappingModel(model, product, false);

				return PartialView(viewPath.NullEmpty() ?? "_CreateOrUpdate." + tabName, model);
			}
			catch (Exception ex)
			{
				return Content("Error while loading template: " + ex.Message);
			}
		}

        //delete product
        [HttpPost]
        public ActionResult Delete(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
                return AccessDeniedView();

            var product = _productService.GetProductById(id);
            _productService.DeleteProduct(product);

            //activity log
            _customerActivityService.InsertActivity("DeleteProduct", _localizationService.GetResource("ActivityLog.DeleteProduct"), product.Name);
                
            NotifySuccess(_localizationService.GetResource("Admin.Catalog.Products.Deleted"));
            return RedirectToAction("List");
        }

        public ActionResult DeleteSelected(string selectedIds)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
                return AccessDeniedView();

            var products = new List<Product>();
            if (selectedIds != null)
            {
                var ids = selectedIds
                    .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => Convert.ToInt32(x))
                    .ToArray();
                products.AddRange(_productService.GetProductsByIds(ids));

                for (int i = 0; i < products.Count; i++)
                {
                    var product = products[i];
                    _productService.DeleteProduct(product);
                }
            }

            return RedirectToAction("List");
        }

        [HttpPost]
        public ActionResult CopyProduct(ProductModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
                return AccessDeniedView();

            var copyModel = model.CopyProductModel;
            try
            {
				var product = _productService.GetProductById(copyModel.Id);
                var newProduct = _copyProductService.CopyProduct(product, copyModel.Name, copyModel.Published, copyModel.CopyImages);

                NotifySuccess("The product is copied");

                return RedirectToAction("Edit", new { id = newProduct.Id });
            }
            catch (Exception exc)
            {
				NotifyError(exc.Message);
                return RedirectToAction("Edit", new { id = copyModel.Id });
            }
        }

        #endregion
        
        #region Product categories

        [HttpPost, GridAction(EnableCustomBinding = true)]
        public ActionResult ProductCategoryList(GridCommand command, int productId)
        {
			var model = new GridModel<ProductModel.ProductCategoryModel>();

			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				var productCategories = _categoryService.GetProductCategoriesByProductId(productId, true);
				var productCategoriesModel = productCategories
					.Select(x =>
					{
						return new ProductModel.ProductCategoryModel()
						{
							Id = x.Id,
							Category = _categoryService.GetCategoryById(x.CategoryId).GetCategoryBreadCrumb(_categoryService),
							ProductId = x.ProductId,
							CategoryId = x.CategoryId,
							IsFeaturedProduct = x.IsFeaturedProduct,
							DisplayOrder = x.DisplayOrder
						};
					})
					.ToList();

				model.Data = productCategoriesModel;
				model.Total = productCategoriesModel.Count;
			}
			else
			{
				model.Data = Enumerable.Empty<ProductModel.ProductCategoryModel>();

				NotifyAccessDenied();
			}

            return new JsonResult
            {
                Data = model
            };
        }

        [GridAction(EnableCustomBinding = true)]
        public ActionResult ProductCategoryInsert(GridCommand command, ProductModel.ProductCategoryModel model)
        {
			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				var productCategory = new ProductCategory
				{
					ProductId = model.ProductId,
					CategoryId = Int32.Parse(model.Category), //use Category property (not CategoryId) because appropriate property is stored in it
					IsFeaturedProduct = model.IsFeaturedProduct,
					DisplayOrder = model.DisplayOrder
				};

				_categoryService.InsertProductCategory(productCategory);

				var mru = new MostRecentlyUsedList<string>(_workContext.CurrentCustomer.GetAttribute<string>(SystemCustomerAttributeNames.MostRecentlyUsedCategories),
					model.Category, _catalogSettings.MostRecentlyUsedCategoriesMaxSize);

				_genericAttributeService.SaveAttribute<string>(_workContext.CurrentCustomer, SystemCustomerAttributeNames.MostRecentlyUsedCategories, mru.ToString());
			}

            return ProductCategoryList(command, model.ProductId);
        }

        [GridAction(EnableCustomBinding = true)]
        public ActionResult ProductCategoryUpdate(GridCommand command, ProductModel.ProductCategoryModel model)
        {
			var productCategory = _categoryService.GetProductCategoryById(model.Id);

			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				var categoryChanged = (Int32.Parse(model.Category) != productCategory.CategoryId);

				//use Category property (not CategoryId) because appropriate property is stored in it
				productCategory.CategoryId = Int32.Parse(model.Category);
				productCategory.IsFeaturedProduct = model.IsFeaturedProduct;
				productCategory.DisplayOrder = model.DisplayOrder;
				_categoryService.UpdateProductCategory(productCategory);

				if (categoryChanged)
				{
					var mru = new MostRecentlyUsedList<string>(_workContext.CurrentCustomer.GetAttribute<string>(SystemCustomerAttributeNames.MostRecentlyUsedCategories),
						model.Category, _catalogSettings.MostRecentlyUsedCategoriesMaxSize);

					_genericAttributeService.SaveAttribute<string>(_workContext.CurrentCustomer, SystemCustomerAttributeNames.MostRecentlyUsedCategories, mru.ToString());
				}
			}

            return ProductCategoryList(command, productCategory.ProductId);
        }

        [GridAction(EnableCustomBinding = true)]
        public ActionResult ProductCategoryDelete(int id, GridCommand command)
        {
			var productCategory = _categoryService.GetProductCategoryById(id);
			var productId = productCategory.ProductId;

			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				_categoryService.DeleteProductCategory(productCategory);
			}

            return ProductCategoryList(command, productId);
        }

        #endregion

        #region Product manufacturers

        [HttpPost, GridAction(EnableCustomBinding = true)]
        public ActionResult ProductManufacturerList(GridCommand command, int productId)
        {
			var model = new GridModel<ProductModel.ProductManufacturerModel>();

			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				var productManufacturers = _manufacturerService.GetProductManufacturersByProductId(productId, true);
				var productManufacturersModel = productManufacturers
					.Select(x =>
					{
						return new ProductModel.ProductManufacturerModel
						{
							Id = x.Id,
							Manufacturer = _manufacturerService.GetManufacturerById(x.ManufacturerId).Name,
							ProductId = x.ProductId,
							ManufacturerId = x.ManufacturerId,
							IsFeaturedProduct = x.IsFeaturedProduct,
							DisplayOrder = x.DisplayOrder
						};
					})
					.ToList();

				model.Data = productManufacturersModel;
				model.Total = productManufacturersModel.Count;
			}
			else
			{
				model.Data = Enumerable.Empty<ProductModel.ProductManufacturerModel>();

				NotifyAccessDenied();
			}

            return new JsonResult
            {
                Data = model
            };
        }

        [GridAction(EnableCustomBinding = true)]
        public ActionResult ProductManufacturerInsert(GridCommand command, ProductModel.ProductManufacturerModel model)
        {
			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				var productManufacturer = new ProductManufacturer
				{
					ProductId = model.ProductId,
					ManufacturerId = Int32.Parse(model.Manufacturer), //use Manufacturer property (not ManufacturerId) because appropriate property is stored in it
					IsFeaturedProduct = model.IsFeaturedProduct,
					DisplayOrder = model.DisplayOrder
				};

				_manufacturerService.InsertProductManufacturer(productManufacturer);

				var mru = new MostRecentlyUsedList<string>(_workContext.CurrentCustomer.GetAttribute<string>(SystemCustomerAttributeNames.MostRecentlyUsedManufacturers),
					model.Manufacturer, _catalogSettings.MostRecentlyUsedManufacturersMaxSize);

				_genericAttributeService.SaveAttribute<string>(_workContext.CurrentCustomer, SystemCustomerAttributeNames.MostRecentlyUsedManufacturers, mru.ToString());
			}

            return ProductManufacturerList(command, model.ProductId);
        }

        [GridAction(EnableCustomBinding = true)]
        public ActionResult ProductManufacturerUpdate(GridCommand command, ProductModel.ProductManufacturerModel model)
        {
			var productManufacturer = _manufacturerService.GetProductManufacturerById(model.Id);

			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				var manufacturerChanged = (Int32.Parse(model.Manufacturer) != productManufacturer.ManufacturerId);

				//use Manufacturer property (not ManufacturerId) because appropriate property is stored in it
				productManufacturer.ManufacturerId = Int32.Parse(model.Manufacturer);
				productManufacturer.IsFeaturedProduct = model.IsFeaturedProduct;
				productManufacturer.DisplayOrder = model.DisplayOrder;

				_manufacturerService.UpdateProductManufacturer(productManufacturer);

				if (manufacturerChanged)
				{
					var mru = new MostRecentlyUsedList<string>(_workContext.CurrentCustomer.GetAttribute<string>(SystemCustomerAttributeNames.MostRecentlyUsedManufacturers),
						model.Manufacturer, _catalogSettings.MostRecentlyUsedManufacturersMaxSize);

					_genericAttributeService.SaveAttribute<string>(_workContext.CurrentCustomer, SystemCustomerAttributeNames.MostRecentlyUsedManufacturers, mru.ToString());
				}
			}

            return ProductManufacturerList(command, productManufacturer.ProductId);
        }

        [GridAction(EnableCustomBinding = true)]
        public ActionResult ProductManufacturerDelete(int id, GridCommand command)
        {
			var productManufacturer = _manufacturerService.GetProductManufacturerById(id);
			var productId = productManufacturer.ProductId;

			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				_manufacturerService.DeleteProductManufacturer(productManufacturer);
			}

            return ProductManufacturerList(command, productId);
        }
        
        #endregion

        #region Related products

        [HttpPost, GridAction(EnableCustomBinding = true)]
        public ActionResult RelatedProductList(GridCommand command, int productId)
        {
			var model = new GridModel<ProductModel.RelatedProductModel>();

			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				var relatedProducts = _productService.GetRelatedProductsByProductId1(productId, true);
				var relatedProductsModel = relatedProducts
					.Select(x =>
					{
						var product2 = _productService.GetProductById(x.ProductId2);

						return new ProductModel.RelatedProductModel()
						{
							Id = x.Id,
							ProductId2 = x.ProductId2,
							Product2Name = product2.Name,
							ProductTypeName = product2.GetProductTypeLabel(_localizationService),
							ProductTypeLabelHint = product2.ProductTypeLabelHint,
							DisplayOrder = x.DisplayOrder,
							Product2Sku = product2.Sku,
							Product2Published = product2.Published
						};
					})
					.ToList();

				model.Data = relatedProductsModel;
				model.Total = relatedProductsModel.Count;
			}
			else
			{
				model.Data = Enumerable.Empty<ProductModel.RelatedProductModel>();

				NotifyAccessDenied();
			}

            return new JsonResult
            {
                Data = model
            };
        }
        
        [GridAction(EnableCustomBinding = true)]
        public ActionResult RelatedProductUpdate(GridCommand command, ProductModel.RelatedProductModel model)
        {
			var relatedProduct = _productService.GetRelatedProductById(model.Id);

			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				relatedProduct.DisplayOrder = model.DisplayOrder;
				_productService.UpdateRelatedProduct(relatedProduct);
			}

            return RelatedProductList(command, relatedProduct.ProductId1);
        }

        [GridAction(EnableCustomBinding = true)]
        public ActionResult RelatedProductDelete(int id, GridCommand command)
        {
			var relatedProduct = _productService.GetRelatedProductById(id);
			var productId = relatedProduct.ProductId1;

			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				_productService.DeleteRelatedProduct(relatedProduct);
			}

            return RelatedProductList(command, productId);
        }
        
        public ActionResult RelatedProductAddPopup(int productId)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
                return AccessDeniedView();

            var ctx = new ProductSearchContext();
            ctx.LanguageId = _workContext.WorkingLanguage.Id;
            ctx.OrderBy = ProductSortingEnum.Position;
            ctx.PageSize = _adminAreaSettings.GridPageSize;
            ctx.ShowHidden = true;

            var products = _productService.SearchProducts(ctx);

            var model = new ProductModel.AddRelatedProductModel();
            model.Products = new GridModel<ProductModel>
            {
                Data = products.Select(x => 
				{
					var productModel = x.ToModel();
					productModel.ProductTypeName = x.GetProductTypeLabel(_localizationService);

					return productModel;
				}),
                Total = products.TotalCount
            };

            var allCategories = _categoryService.GetAllCategories(showHidden: true);
            var mappedCategories = allCategories.ToDictionary(x => x.Id);
            foreach (var c in allCategories)
            {
                model.AvailableCategories.Add(new SelectListItem { Text = c.GetCategoryNameWithPrefix(_categoryService, mappedCategories), Value = c.Id.ToString() });
            }

            foreach (var m in _manufacturerService.GetAllManufacturers(true))
            {
                model.AvailableManufacturers.Add(new SelectListItem { Text = m.Name, Value = m.Id.ToString() });
            }

			foreach (var s in _storeService.GetAllStores())
			{
				model.AvailableStores.Add(new SelectListItem { Text = s.Name, Value = s.Id.ToString() });
			}

			model.AvailableProductTypes = ProductType.SimpleProduct.ToSelectList(false).ToList();

            return View(model);
        }

        [HttpPost, GridAction(EnableCustomBinding = true)]
        public ActionResult RelatedProductAddPopupList(GridCommand command, ProductModel.AddRelatedProductModel model)
        {
			var gridModel = new GridModel<ProductModel>();

			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				var ctx = new ProductSearchContext();

				if (model.SearchCategoryId > 0)
					ctx.CategoryIds.Add(model.SearchCategoryId);

				ctx.ManufacturerId = model.SearchManufacturerId;
				ctx.StoreId = model.SearchStoreId;
				ctx.Keywords = model.SearchProductName;
				ctx.SearchSku = !_catalogSettings.SuppressSkuSearch;
				ctx.LanguageId = _workContext.WorkingLanguage.Id;
				ctx.OrderBy = ProductSortingEnum.Position;
				ctx.PageIndex = command.Page - 1;
				ctx.PageSize = command.PageSize;
				ctx.ShowHidden = true;
				ctx.ProductType = model.SearchProductTypeId > 0 ? (ProductType?)model.SearchProductTypeId : null;

				var products = _productService.SearchProducts(ctx);

				gridModel.Data = products.Select(x =>
				{
                    var productModel = new ProductModel
                    {
                        Sku = x.Sku,
                        Published = x.Published,
                        ProductTypeLabelHint = x.ProductTypeLabelHint,
                        Name = x.Name,
                        Id = x.Id
                    };

					productModel.ProductTypeName = x.GetProductTypeLabel(_localizationService);

					return productModel;
				});

				gridModel.Total = products.TotalCount;
			}
			else
			{
				gridModel.Data = Enumerable.Empty<ProductModel>();

				NotifyAccessDenied();
			}

            return new JsonResult
            {
                Data = gridModel
            };
        }

        [HttpPost]
        [FormValueRequired("save")]
        public ActionResult RelatedProductAddPopup(string btnId, string formId, ProductModel.AddRelatedProductModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
                return AccessDeniedView();

            if (model.SelectedProductIds != null)
            {
                foreach (int id in model.SelectedProductIds)
                {
                    var product = _productService.GetProductById(id);
                    if (product != null)
                    {
                        var existingRelatedProducts = _productService.GetRelatedProductsByProductId1(model.ProductId);
                        if (existingRelatedProducts.FindRelatedProduct(model.ProductId, id) == null)
                        {
                            _productService.InsertRelatedProduct(
                                new RelatedProduct()
                                {
                                    ProductId1 = model.ProductId,
                                    ProductId2 = id,
                                    DisplayOrder = 1
                                });
                        }
                    }
                }
            }

            ViewBag.RefreshPage = true;
            ViewBag.btnId = btnId;
            ViewBag.formId = formId;
            model.Products = new GridModel<ProductModel>();
            return View(model);
        }

		[HttpPost]
		[ValidateInput(false)]
		public ActionResult CreateAllMutuallyRelatedProducts(int productId)
		{
			string message = null;

			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				var product = _productService.GetProductById(productId);
				if (product != null)
				{
					int count = _productService.EnsureMutuallyRelatedProducts(productId);
					message = T("Admin.Common.CreateMutuallyAssociationsResult", count);
				}
				else
				{
					message = "No product found with the specified id";
				}
			}
			else
			{
				message = T("Admin.AccessDenied.Title");
			}

			return new JsonResult
			{
				Data = new { Message = message }
			};
		}
        
        #endregion

        #region Cross-sell products

        [HttpPost, GridAction(EnableCustomBinding = true)]
        public ActionResult CrossSellProductList(GridCommand command, int productId)
        {
			var model = new GridModel<ProductModel.CrossSellProductModel>();

			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				var crossSellProducts = _productService.GetCrossSellProductsByProductId1(productId, true);
				var crossSellProductsModel = crossSellProducts
					.Select(x =>
					{
						var product2 = _productService.GetProductById(x.ProductId2);

						return new ProductModel.CrossSellProductModel
						{
							Id = x.Id,
							ProductId2 = x.ProductId2,
							Product2Name = product2.Name,
							ProductTypeName = product2.GetProductTypeLabel(_localizationService),
							ProductTypeLabelHint = product2.ProductTypeLabelHint,
							Product2Sku = product2.Sku,
							Product2Published = product2.Published
						};
					})
					.ToList();

				model.Data = crossSellProductsModel;
				model.Total = crossSellProductsModel.Count;
			}
			else
			{
				model.Data = Enumerable.Empty<ProductModel.CrossSellProductModel>();

				NotifyAccessDenied();
			}

            return new JsonResult
            {
                Data = model
            };
        }

        [GridAction(EnableCustomBinding = true)]
        public ActionResult CrossSellProductDelete(int id, GridCommand command)
        {
			var crossSellProduct = _productService.GetCrossSellProductById(id);
			var productId = crossSellProduct.ProductId1;

			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				_productService.DeleteCrossSellProduct(crossSellProduct);
			}

            return CrossSellProductList(command, productId);
        }

        public ActionResult CrossSellProductAddPopup(int productId)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
                return AccessDeniedView();

            var ctx = new ProductSearchContext();
            ctx.LanguageId = _workContext.WorkingLanguage.Id;
            ctx.OrderBy = ProductSortingEnum.Position;
            ctx.PageSize = _adminAreaSettings.GridPageSize;
            ctx.ShowHidden = true;

            var products = _productService.SearchProducts(ctx);

            var model = new ProductModel.AddCrossSellProductModel();
            model.Products = new GridModel<ProductModel>
            {
                Data = products.Select(x => 
				{
					var productModel = x.ToModel();
					productModel.ProductTypeName = x.GetProductTypeLabel(_localizationService);

					return productModel;
				}),
                Total = products.TotalCount
            };

            var allCategories = _categoryService.GetAllCategories(showHidden: true);
            var mappedCategories = allCategories.ToDictionary(x => x.Id);
            foreach (var c in allCategories)
            {
                model.AvailableCategories.Add(new SelectListItem { Text = c.GetCategoryNameWithPrefix(_categoryService, mappedCategories), Value = c.Id.ToString() });
            }

            foreach (var m in _manufacturerService.GetAllManufacturers(true))
            {
                model.AvailableManufacturers.Add(new SelectListItem { Text = m.Name, Value = m.Id.ToString() });
            }

			foreach (var s in _storeService.GetAllStores())
			{
				model.AvailableStores.Add(new SelectListItem { Text = s.Name, Value = s.Id.ToString() });
			}

			model.AvailableProductTypes = ProductType.SimpleProduct.ToSelectList(false).ToList();

            return View(model);
        }

        [HttpPost, GridAction(EnableCustomBinding = true)]
        public ActionResult CrossSellProductAddPopupList(GridCommand command, ProductModel.AddCrossSellProductModel model)
        {
			var gridModel = new GridModel<ProductModel>();

			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				var ctx = new ProductSearchContext();

				if (model.SearchCategoryId > 0)
					ctx.CategoryIds.Add(model.SearchCategoryId);

				ctx.ManufacturerId = model.SearchManufacturerId;
				ctx.StoreId = model.SearchStoreId;
				ctx.Keywords = model.SearchProductName;
				ctx.SearchSku = !_catalogSettings.SuppressSkuSearch;
				ctx.LanguageId = _workContext.WorkingLanguage.Id;
				ctx.OrderBy = ProductSortingEnum.Position;
				ctx.PageIndex = command.Page - 1;
				ctx.PageSize = command.PageSize;
				ctx.ShowHidden = true;
				ctx.ProductType = model.SearchProductTypeId > 0 ? (ProductType?)model.SearchProductTypeId : null;

				var products = _productService.SearchProducts(ctx);

				gridModel.Data = products.Select(x =>
				{
                    var productModel = new ProductModel
                    {
                        Sku = x.Sku,
                        Published = x.Published,
                        ProductTypeLabelHint = x.ProductTypeLabelHint,
                        Name = x.Name,
                        Id = x.Id
                    };

					productModel.ProductTypeName = x.GetProductTypeLabel(_localizationService);

					return productModel;
				});

				gridModel.Total = products.TotalCount;
			}
			else
			{
				gridModel.Data = Enumerable.Empty<ProductModel>();

				NotifyAccessDenied();
			}

            return new JsonResult
            {
                Data = gridModel
            };
        }

        [HttpPost]
        [FormValueRequired("save")]
        public ActionResult CrossSellProductAddPopup(string btnId, string formId, ProductModel.AddCrossSellProductModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
                return AccessDeniedView();

            if (model.SelectedProductIds != null)
            {
                foreach (int id in model.SelectedProductIds)
                {
                    var product = _productService.GetProductById(id);
                    if (product != null)
                    {
                        var existingCrossSellProducts = _productService.GetCrossSellProductsByProductId1(model.ProductId);
                        if (existingCrossSellProducts.FindCrossSellProduct(model.ProductId, id) == null)
                        {
                            _productService.InsertCrossSellProduct(
                                new CrossSellProduct()
                                {
                                    ProductId1 = model.ProductId,
                                    ProductId2 = id,
                                });
                        }
                    }
                }
            }

            ViewBag.RefreshPage = true;
            ViewBag.btnId = btnId;
            ViewBag.formId = formId;
            model.Products = new GridModel<ProductModel>();
            return View(model);
        }

		[HttpPost]
		[ValidateInput(false)]
		public ActionResult CreateAllMutuallyCrossSellProducts(int productId)
		{
			string message = null;

			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				var product = _productService.GetProductById(productId);
				if (product != null)
				{
					int count = _productService.EnsureMutuallyCrossSellProducts(productId);
					message = T("Admin.Common.CreateMutuallyAssociationsResult", count);
				}
				else
				{
					message = "No product found with the specified id";
				}
			}
			else
			{
				message = T("Admin.AccessDenied.Title");
			}

			return new JsonResult
			{
				Data = new { Message = message }
			};
		}

        #endregion

		#region Associated products

		[HttpPost, GridAction(EnableCustomBinding = true)]
		public ActionResult AssociatedProductList(GridCommand command, int productId)
		{
			var model = new GridModel<ProductModel.AssociatedProductModel>();

			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				var searchContext = new ProductSearchContext
				{
					ParentGroupedProductId = productId,
					PageSize = int.MaxValue,
					ShowHidden = true
				};

				var associatedProducts = _productService.SearchProducts(searchContext);
				var associatedProductsModel = associatedProducts
					.Select(x =>
					{
						return new ProductModel.AssociatedProductModel
						{
							Id = x.Id,
							ProductName = x.Name,
							ProductTypeName = x.GetProductTypeLabel(_localizationService),
							ProductTypeLabelHint = x.ProductTypeLabelHint,
							DisplayOrder = x.DisplayOrder,
							Sku = x.Sku,
							Published = x.Published
						};
					})
					.ToList();

				model.Data = associatedProductsModel;
				model.Total = associatedProductsModel.Count;
			}
			else
			{
				model.Data = Enumerable.Empty<ProductModel.AssociatedProductModel>();

				NotifyAccessDenied();
			}

			return new JsonResult
			{
				Data = model
			};
		}

		[GridAction(EnableCustomBinding = true)]
		public ActionResult AssociatedProductUpdate(GridCommand command, ProductModel.AssociatedProductModel model)
		{
			var associatedProduct = _productService.GetProductById(model.Id);

			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				associatedProduct.DisplayOrder = model.DisplayOrder;
				_productService.UpdateProduct(associatedProduct);
			}

			return AssociatedProductList(command, associatedProduct.ParentGroupedProductId);
		}

		[GridAction(EnableCustomBinding = true)]
		public ActionResult AssociatedProductDelete(int id, GridCommand command)
		{
			var product = _productService.GetProductById(id);
			var originalParentGroupedProductId = product.ParentGroupedProductId;

			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				product.ParentGroupedProductId = 0;
				_productService.UpdateProduct(product);
			}

			return AssociatedProductList(command, originalParentGroupedProductId);
		}

		public ActionResult AssociatedProductAddPopup(int productId)
		{
			if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
				return AccessDeniedView();

			var model = new ProductModel.AddAssociatedProductModel();

			var allCategories = _categoryService.GetAllCategories(showHidden: true);
			var mappedCategories = allCategories.ToDictionary(x => x.Id);
			foreach (var c in allCategories)
			{
				model.AvailableCategories.Add(new SelectListItem { Text = c.GetCategoryNameWithPrefix(_categoryService, mappedCategories), Value = c.Id.ToString() });
			}

			foreach (var m in _manufacturerService.GetAllManufacturers(true))
			{
				model.AvailableManufacturers.Add(new SelectListItem { Text = m.Name, Value = m.Id.ToString() });
			}

			foreach (var s in _storeService.GetAllStores())
			{
				model.AvailableStores.Add(new SelectListItem { Text = s.Name, Value = s.Id.ToString() });
			}

			model.AvailableProductTypes = ProductType.SimpleProduct.ToSelectList(false).ToList();

			return View(model);
		}

		[HttpPost, GridAction(EnableCustomBinding = true)]
		public ActionResult AssociatedProductAddPopupList(GridCommand command, ProductModel.AddAssociatedProductModel model)
		{
			var gridModel = new GridModel<ProductModel>();

			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				var searchContext = new ProductSearchContext
				{
					CategoryIds = new List<int>() { model.SearchCategoryId },
					ManufacturerId = model.SearchManufacturerId,
					StoreId = model.SearchStoreId,
					Keywords = model.SearchProductName,
					SearchSku = !_catalogSettings.SuppressSkuSearch,
					PageIndex = command.Page - 1,
					PageSize = command.PageSize,
					ShowHidden = true,
					ProductType = model.SearchProductTypeId > 0 ? (ProductType?)model.SearchProductTypeId : null
				};

				var products = _productService.SearchProducts(searchContext);

				gridModel.Data = products.Select(x =>
				{
                    var productModel = new ProductModel
                    {
                        Sku = x.Sku,
                        Published = x.Published,
                        ProductTypeLabelHint = x.ProductTypeLabelHint,
                        Name = x.Name,
                        Id = x.Id
                    };

					productModel.ProductTypeName = x.GetProductTypeLabel(_localizationService);

					return productModel;
				});

				gridModel.Total = products.TotalCount;
			}
			else
			{
				gridModel.Data = Enumerable.Empty<ProductModel>();

				NotifyAccessDenied();
			}

			return new JsonResult
			{
				Data = gridModel
			};
		}

		[HttpPost]
		[FormValueRequired("save")]
		public ActionResult AssociatedProductAddPopup(string btnId, string formId, ProductModel.AddAssociatedProductModel model)
		{
			if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
				return AccessDeniedView();

			if (model.SelectedProductIds != null)
			{
				foreach (int id in model.SelectedProductIds)
				{
					var product = _productService.GetProductById(id);
					if (product != null)
					{
						product.ParentGroupedProductId = model.ProductId;
						_productService.UpdateProduct(product);
					}
				}
			}

			ViewBag.RefreshPage = true;
			ViewBag.btnId = btnId;
			ViewBag.formId = formId;
			return View(model);
		}

		#endregion

		#region Bundle items

		private void PrepareBundleItemEditModel(ProductBundleItemModel model, ProductBundleItem bundleItem, string btnId, string formId, bool refreshPage = false)
		{
			ViewBag.BtnId = btnId;
			ViewBag.FormId = formId;
			ViewBag.RefreshPage = refreshPage;

			if (bundleItem == null)
			{
				ViewBag.Title = _localizationService.GetResource("Admin.Catalog.Products.BundleItems.EditOf");
				return;
			}

			model.CreatedOn = _dateTimeHelper.ConvertToUserTime(bundleItem.CreatedOnUtc, DateTimeKind.Utc);
			model.UpdatedOn = _dateTimeHelper.ConvertToUserTime(bundleItem.UpdatedOnUtc, DateTimeKind.Utc);
			model.IsPerItemPricing = bundleItem.BundleProduct.BundlePerItemPricing;

			if (model.Locales.Count == 0)
			{
				AddLocales(_languageService, model.Locales, (locale, languageId) =>
				{
					locale.Name = bundleItem.GetLocalized(x => x.Name, languageId, false, false);
					locale.ShortDescription = bundleItem.GetLocalized(x => x.ShortDescription, languageId, false, false);
				});
			}

			ViewBag.Title = "{0} {1} ({2})".FormatWith(
				_localizationService.GetResource("Admin.Catalog.Products.BundleItems.EditOf"), bundleItem.Product.Name, bundleItem.Product.Sku);

			var attributes = _productAttributeService.GetProductVariantAttributesByProductId(bundleItem.ProductId);

			foreach (var attribute in attributes)
			{
				var attributeModel = new ProductBundleItemAttributeModel()
				{
					Id = attribute.Id,
					Name = (attribute.ProductAttribute.Alias.HasValue() ?
						"{0} ({1})".FormatWith(attribute.ProductAttribute.Name, attribute.ProductAttribute.Alias) : attribute.ProductAttribute.Name)
				};

				var attributeValues = _productAttributeService.GetProductVariantAttributeValues(attribute.Id);

				foreach (var attributeValue in attributeValues)
				{
					var filteredValue = bundleItem.AttributeFilters.FirstOrDefault(x => x.AttributeId == attribute.Id && x.AttributeValueId == attributeValue.Id);

					attributeModel.Values.Add(new SelectListItem()
					{
						Text = attributeValue.Name,
						Value = attributeValue.Id.ToString(),
						Selected = (filteredValue != null)
					});

					if (filteredValue != null)
					{
						attributeModel.PreSelect.Add(new SelectListItem()
						{
							Text = attributeValue.Name,
							Value = attributeValue.Id.ToString(),
							Selected = filteredValue.IsPreSelected
						});
					}
				}

				if (attributeModel.Values.Count > 0)
				{
					if (attributeModel.PreSelect.Count > 0)
						attributeModel.PreSelect.Insert(0, new SelectListItem() { Text = _localizationService.GetResource("Admin.Common.PleaseSelect") });

					model.Attributes.Add(attributeModel);
				}
			}
		}
		private void SaveFilteredAttributes(ProductBundleItem bundleItem, FormCollection form)
		{
			_productAttributeService.DeleteProductBundleItemAttributeFilter(bundleItem);

			var allFilterKeys = form.AllKeys.Where(x => x.HasValue() && x.StartsWith(ProductBundleItemAttributeModel.AttributeControlPrefix));

			foreach (var key in allFilterKeys)
			{
				int attributeId = key.Substring(ProductBundleItemAttributeModel.AttributeControlPrefix.Length).ToInt();
				string preSelectId = form[ProductBundleItemAttributeModel.PreSelectControlPrefix + attributeId.ToString()].EmptyNull();

				foreach (var valueId in form[key].SplitSafe(","))
				{
					var attributeFilter = new ProductBundleItemAttributeFilter()
					{
						BundleItemId = bundleItem.Id,
						AttributeId = attributeId,
						AttributeValueId = valueId.ToInt(),
						IsPreSelected = (preSelectId == valueId)
					};

					_productAttributeService.InsertProductBundleItemAttributeFilter(attributeFilter);
				}
			}
		}

		[HttpPost, GridAction(EnableCustomBinding = true)]
		public ActionResult BundleItemList(GridCommand command, int productId)
		{
			var model = new GridModel<ProductModel.BundleItemModel>();

			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				var bundleItems = _productService.GetBundleItems(productId, true).Select(x => x.Item);

				var bundleItemsModel = bundleItems.Select(x =>
					{
						return new ProductModel.BundleItemModel
						{
							Id = x.Id,
							ProductId = x.Product.Id,
							ProductName = x.Product.Name,
							ProductTypeName = x.Product.GetProductTypeLabel(_localizationService),
							ProductTypeLabelHint = x.Product.ProductTypeLabelHint,
							Sku = x.Product.Sku,
							Quantity = x.Quantity,
							Discount = x.Discount,
							DisplayOrder = x.DisplayOrder,
							Visible = x.Visible,
							Published = x.Published
						};
					}).ToList();

				model.Data = bundleItemsModel;
				model.Total = bundleItemsModel.Count;
			}
			else
			{
				model.Data = Enumerable.Empty<ProductModel.BundleItemModel>();

				NotifyAccessDenied();
			}

			return new JsonResult { Data = model };
		}

		[GridAction(EnableCustomBinding = true)]
		public ActionResult BundleItemDelete(int id, GridCommand command)
		{
			var bundleItem = _productService.GetBundleItemById(id);

			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				_productService.DeleteBundleItem(bundleItem);
			}

			return BundleItemList(command, bundleItem.BundleProductId);
		}

		public ActionResult BundleItemAddPopup(int productId)
		{
			if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
				return AccessDeniedView();

			var product = _productService.GetProductById(productId);

			if (product.ProductType != ProductType.BundledProduct)
				throw new ArgumentException("Bundle items can only be added to bundles.");

			var model = new ProductModel.AddBundleItemModel()
			{
				IsPerItemPricing = product.BundlePerItemPricing,
				IsPerItemShipping = product.BundlePerItemShipping
			};

			var allCategories = _categoryService.GetAllCategories(showHidden: true);
			var mappedCategories = allCategories.ToDictionary(x => x.Id);
			foreach (var c in allCategories)
			{
				model.AvailableCategories.Add(new SelectListItem { Text = c.GetCategoryNameWithPrefix(_categoryService, mappedCategories), Value = c.Id.ToString() });
			}

			foreach (var m in _manufacturerService.GetAllManufacturers(true))
			{
				model.AvailableManufacturers.Add(new SelectListItem { Text = m.Name, Value = m.Id.ToString() });
			}

			foreach (var s in _storeService.GetAllStores())
			{
				model.AvailableStores.Add(new SelectListItem { Text = s.Name, Value = s.Id.ToString() });
			}

			model.AvailableProductTypes = ProductType.SimpleProduct.ToSelectList(false).ToList();

			ViewBag.RefreshPage = false;
			ViewBag.CloseWindow = false;

			return View(model);
		}

		[HttpPost]
		[FormValueRequired("save")]
		public ActionResult BundleItemAddPopup(string btnId, string formId, ProductModel.AddBundleItemModel model)
		{
			if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
				return AccessDeniedView();

			bool closeWindow = true;

			if (model.SelectedProductIds != null)
			{
				var products = _productService.GetProductsByIds(model.SelectedProductIds);
				var utcNow = DateTime.UtcNow;

				foreach (var product in products.Where(x => x.CanBeBundleItem()))
				{
					var attributes = _productAttributeService.GetProductVariantAttributesByProductId(product.Id);

					if (attributes.Count > 0 && attributes.Any(a => a.ProductVariantAttributeValues.Any(v => v.ValueType == ProductVariantAttributeValueType.ProductLinkage)))
					{
						NotifyError(_localizationService.GetResource("Admin.Catalog.Products.BundleItems.NoAttributeWithProductLinkage"));
						closeWindow = false;
					}
					else
					{
						var bundleItem = new ProductBundleItem()
						{
							ProductId = product.Id,
							BundleProductId = model.ProductId,
							Quantity = 1,
							Visible = true,
							Published = true,
							DisplayOrder = products.IndexOf(product) + 1,
							CreatedOnUtc = utcNow,
							UpdatedOnUtc = utcNow
						};

						_productService.InsertBundleItem(bundleItem);
					}
				}
			}

			ViewBag.RefreshPage = true;
			ViewBag.CloseWindow = closeWindow;
			ViewBag.btnId = btnId;
			ViewBag.formId = formId;
			return View(model);
		}

		[HttpPost, GridAction(EnableCustomBinding = true)]
		public ActionResult BundleItemAddPopupList(GridCommand command, ProductModel.AddBundleItemModel model)
		{
			var gridModel = new GridModel<ProductModel>();

			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				var searchContext = new ProductSearchContext
				{
					CategoryIds = new List<int> { model.SearchCategoryId },
					ManufacturerId = model.SearchManufacturerId,
					StoreId = model.SearchStoreId,
					Keywords = model.SearchProductName,
					SearchSku = !_catalogSettings.SuppressSkuSearch,
					PageIndex = command.Page - 1,
					PageSize = command.PageSize,
					ShowHidden = true,
					ProductType = model.SearchProductTypeId > 0 ? (ProductType?)model.SearchProductTypeId : null
				};

				var products = _productService.SearchProducts(searchContext);

				gridModel.Data = products.Select(x =>
				{
                    var productModel = new ProductModel
                    {
                        Sku = x.Sku,
                        Published = x.Published,
                        ProductTypeLabelHint = x.ProductTypeLabelHint,
                        Name = x.Name,
                        Id = x.Id
                    };

					productModel.ProductTypeName = x.GetProductTypeLabel(_localizationService);
					productModel.ProductSelectCheckboxClass = (!x.CanBeBundleItem() ? " hide" : "");

					return productModel;
				});

				gridModel.Total = products.TotalCount;
			}
			else
			{
				gridModel.Data = Enumerable.Empty<ProductModel>();

				NotifyAccessDenied();
			}

			return new JsonResult
			{
				Data = gridModel
			};
		}

		public ActionResult BundleItemEditPopup(int id, string btnId, string formId)
		{
			if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
				return AccessDeniedView();

			var bundleItem = _productService.GetBundleItemById(id);

			if (bundleItem == null)
				throw new ArgumentException("No bundle item found with the specified id");

			var model = bundleItem.ToModel();

			PrepareBundleItemEditModel(model, bundleItem, btnId, formId);

			return View(model);
		}

		[ValidateInput(false)]
		[HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
		public ActionResult BundleItemEditPopup(string btnId, string formId, bool continueEditing, ProductBundleItemModel model, FormCollection form)
		{
			if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
				return AccessDeniedView();

			ViewBag.CloseWindow = !continueEditing;

			if (ModelState.IsValid)
			{
				var bundleItem = _productService.GetBundleItemById(model.Id);

				if (bundleItem == null)
					throw new ArgumentException("No bundle item found with the specified id");

				bundleItem = model.ToEntity(bundleItem);
				bundleItem.UpdatedOnUtc = DateTime.UtcNow;

				_productService.UpdateBundleItem(bundleItem);

				foreach (var localized in model.Locales)
				{
					_localizedEntityService.SaveLocalizedValue(bundleItem, x => x.Name, localized.Name, localized.LanguageId);
					_localizedEntityService.SaveLocalizedValue(bundleItem, x => x.ShortDescription, localized.ShortDescription, localized.LanguageId);
				}

				if (bundleItem.FilterAttributes)	// only update filters if attribute filtering is activated to reduce payload
					SaveFilteredAttributes(bundleItem, form);

				PrepareBundleItemEditModel(model, bundleItem, btnId, formId, true);

				if (continueEditing)
					this.NotifySuccess(_localizationService.GetResource("Admin.Common.DataSuccessfullySaved"));
			}
			else
			{
				PrepareBundleItemEditModel(model, null, btnId, formId);
			}
			return View(model);
		}

		#endregion

		#region Product pictures

		public ActionResult ProductPictureAdd(int pictureId, int displayOrder, int productId)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
                return AccessDeniedView();

            if (pictureId == 0)
                throw new ArgumentException();

            var product = _productService.GetProductById(productId);
            if (product == null)
                throw new ArgumentException("No product found with the specified id");

			var productPicture = new ProductPicture
            {
                PictureId = pictureId,
                ProductId = productId,
                DisplayOrder = displayOrder,
            };

			MediaHelper.UpdatePictureTransientStateFor(productPicture, pp => pp.PictureId);

            _productService.InsertProductPicture(productPicture);

            _pictureService.SetSeoFilename(pictureId, _pictureService.GetPictureSeName(product.Name));

            return Json(new { Result = true }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost, GridAction(EnableCustomBinding = true)]
        public ActionResult ProductPictureList(GridCommand command, int productId)
        {
			var model = new GridModel<ProductModel.ProductPictureModel>();

			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				var productPictures = _productService.GetProductPicturesByProductId(productId);
				var productPicturesModel = productPictures
					.Select(x =>
					{
						return new ProductModel.ProductPictureModel
						{
							Id = x.Id,
							ProductId = x.ProductId,
							PictureId = x.PictureId,
							PictureUrl = _pictureService.GetPictureUrl(x.PictureId),
							DisplayOrder = x.DisplayOrder
						};
					})
					.ToList();

				model.Data = productPicturesModel;
				model.Total = productPicturesModel.Count;
			}
			else
			{
				model.Data = Enumerable.Empty<ProductModel.ProductPictureModel>();

				NotifyAccessDenied();
			}

            return new JsonResult
            {
                Data = model
            };
        }

        [GridAction(EnableCustomBinding = true)]
        public ActionResult ProductPictureUpdate(ProductModel.ProductPictureModel model, GridCommand command)
        {
			var productPicture = _productService.GetProductPictureById(model.Id);

			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				productPicture.DisplayOrder = model.DisplayOrder;
				_productService.UpdateProductPicture(productPicture);
			}

            return ProductPictureList(command, productPicture.ProductId);
        }

        [GridAction(EnableCustomBinding = true)]
        public ActionResult ProductPictureDelete(int id, GridCommand command)
        {
			var productPicture = _productService.GetProductPictureById(id);
			var productId = productPicture.ProductId;

			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				_productService.DeleteProductPicture(productPicture);

				var picture = _pictureService.GetPictureById(productPicture.PictureId);
				_pictureService.DeletePicture(picture);
			}
            
            return ProductPictureList(command, productId);
        }

        #endregion

        #region Product specification attributes

        public ActionResult ProductSpecificationAttributeAdd(int specificationAttributeOptionId, 
            bool allowFiltering, bool showOnProductPage, int displayOrder, int productId)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
                return AccessDeniedView();

            var psa = new ProductSpecificationAttribute()
            {
                SpecificationAttributeOptionId = specificationAttributeOptionId,
                ProductId = productId,
                AllowFiltering = allowFiltering,
                ShowOnProductPage = showOnProductPage,
                DisplayOrder = displayOrder,
            };
            _specificationAttributeService.InsertProductSpecificationAttribute(psa);

            return Json(new { Result = true }, JsonRequestBehavior.AllowGet);
        }
        
        [HttpPost, GridAction(EnableCustomBinding = true)]
        public ActionResult ProductSpecAttrList(GridCommand command, int productId)
        {
			var model = new GridModel<ProductSpecificationAttributeModel>();

			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				var productrSpecs = _specificationAttributeService.GetProductSpecificationAttributesByProductId(productId);

				var productrSpecsModel = productrSpecs
					.Select(x =>
					{
						var psaModel = new ProductSpecificationAttributeModel
						{
							Id = x.Id,
							SpecificationAttributeName = x.SpecificationAttributeOption.SpecificationAttribute.Name,
							SpecificationAttributeOptionName = x.SpecificationAttributeOption.Name,
							SpecificationAttributeOptionAttributeId = x.SpecificationAttributeOption.SpecificationAttributeId,
							SpecificationAttributeOptionId = x.SpecificationAttributeOptionId,
							AllowFiltering = x.AllowFiltering,
							ShowOnProductPage = x.ShowOnProductPage,
							DisplayOrder = x.DisplayOrder
						};
						return psaModel;
					})
					.ToList();

				foreach (var attr in productrSpecsModel)
				{
					var options = _specificationAttributeService.GetSpecificationAttributeOptionsBySpecificationAttribute(attr.SpecificationAttributeOptionAttributeId);

					foreach (var option in options)
					{
						attr.SpecificationAttributeOptions.Add(new ProductSpecificationAttributeModel.SpecificationAttributeOption
						{
							id = option.Id,
							name = option.Name,
							text = option.Name
						});
					}

					attr.SpecificationAttributeOptionsJsonString = JsonConvert.SerializeObject(attr.SpecificationAttributeOptions);
				}

				model.Data = productrSpecsModel;
				model.Total = productrSpecsModel.Count;
			}
			else
			{
				model.Data = Enumerable.Empty<ProductSpecificationAttributeModel>();

				NotifyAccessDenied();
			}

            return new JsonResult
            {
                Data = model
            };
        }

        [GridAction(EnableCustomBinding = true)]
        public ActionResult ProductSpecAttrUpdate(int psaId, ProductSpecificationAttributeModel model, GridCommand command)
        {
			var psa = _specificationAttributeService.GetProductSpecificationAttributeById(psaId);

			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				psa.AllowFiltering = model.AllowFiltering;
				psa.ShowOnProductPage = model.ShowOnProductPage;
				psa.DisplayOrder = model.DisplayOrder;
				_specificationAttributeService.UpdateProductSpecificationAttribute(psa);
			}

            return ProductSpecAttrList(command, psa.ProductId);
        }

        [GridAction(EnableCustomBinding = true)]
        public ActionResult ProductSpecAttrDelete(int psaId, GridCommand command)
        {
			var psa = _specificationAttributeService.GetProductSpecificationAttributeById(psaId);
			var productId = psa.ProductId;

			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				_specificationAttributeService.DeleteProductSpecificationAttribute(psa);
			}

            return ProductSpecAttrList(command, productId);
        }

        #endregion

        #region Product tags

        public ActionResult ProductTags()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
                return AccessDeniedView();

            return View();
        }

        [HttpPost, GridAction(EnableCustomBinding = true)]
        public ActionResult ProductTags(GridCommand command)
        {
			var model = new GridModel<ProductTagModel>();

			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				var tags = _productTagService.GetAllProductTags()
					.OrderByDescending(x => _productTagService.GetProductCount(x.Id, 0))
					.Select(x =>
					{
						return new ProductTagModel
						{
							Id = x.Id,
							Name = x.Name,
							ProductCount = _productTagService.GetProductCount(x.Id, 0)
						};
					})
					.ForCommand(command);

				model.Data = tags.PagedForCommand(command);
				model.Total = tags.Count();
			}
			else
			{
				model.Data = Enumerable.Empty<ProductTagModel>();

				NotifyAccessDenied();
			}

            return new JsonResult
            {
                Data = model
            };
        }

        [GridAction(EnableCustomBinding = true)]
        public ActionResult ProductTagDelete(int id, GridCommand command)
        {
			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				var tag = _productTagService.GetProductTagById(id);

				_productTagService.DeleteProductTag(tag);
			}

            return ProductTags(command);
        }

        //edit
        public ActionResult EditProductTag(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
                return AccessDeniedView();

            var productTag = _productTagService.GetProductTagById(id);
            if (productTag == null)
                //No product tag found with the specified id
                return RedirectToAction("List");

            var model = new ProductTagModel()
            {
                Id = productTag.Id,
                Name = productTag.Name,
				ProductCount = _productTagService.GetProductCount(productTag.Id, 0)
            };
            //locales
            AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.Name = productTag.GetLocalized(x => x.Name, languageId, false, false);
            });

            return View(model);
        }

        [HttpPost]
        public ActionResult EditProductTag(string btnId, string formId, ProductTagModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
                return AccessDeniedView();

            var productTag = _productTagService.GetProductTagById(model.Id);
            if (productTag == null)
                //No product tag found with the specified id
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                productTag.Name = model.Name;
                _productTagService.UpdateProductTag(productTag);
                //locales
                UpdateLocales(productTag, model);

                ViewBag.RefreshPage = true;
                ViewBag.btnId = btnId;
                ViewBag.formId = formId;
                return View(model);
            }

            //If we got this far, something failed, redisplay form
            return View(model);
        }

		#endregion

		#region Low stock reports

		public ActionResult LowStockReport()
		{
			if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
				return AccessDeniedView();

			return View();
		}

		[HttpPost, GridAction(EnableCustomBinding = true)]
		public ActionResult LowStockReportList(GridCommand command)
		{
			var model = new GridModel<ProductModel>();

			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				var allProducts = _productService.GetLowStockProducts();

				model.Data = allProducts.PagedForCommand(command).Select(x =>
				{
					var productModel = x.ToModel();
					productModel.ProductTypeName = x.GetProductTypeLabel(_localizationService);

					return productModel;
				});

				model.Total = allProducts.Count;
			}
			else
			{
				model.Data = Enumerable.Empty<ProductModel>();

				NotifyAccessDenied();
			}

			return new JsonResult
			{
				Data = model
			};
		}

		#endregion

		#region Bulk editing

		public ActionResult BulkEdit()
		{
			if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
				return AccessDeniedView();

			var allStores = _services.StoreService.GetAllStores();

			var model = new BulkEditListModel();
			model.GridPageSize = _adminAreaSettings.GridPageSize;

			var allCategories = _categoryService.GetAllCategories(showHidden: true);
			var mappedCategories = allCategories.ToDictionary(x => x.Id);
			foreach (var c in allCategories)
			{
				model.AvailableCategories.Add(new SelectListItem { Text = c.GetCategoryNameWithPrefix(_categoryService, mappedCategories), Value = c.Id.ToString() });
			}

			foreach (var m in _manufacturerService.GetAllManufacturers(true))
			{
				model.AvailableManufacturers.Add(new SelectListItem { Text = m.Name, Value = m.Id.ToString() });
			}

			foreach (var s in allStores)
			{
				model.AvailableStores.Add(new SelectListItem { Text = s.Name, Value = s.Id.ToString() });
			}

			model.AvailableProductTypes = ProductType.SimpleProduct.ToSelectList(false).ToList();

			return View(model);
		}

		[HttpPost, GridAction(EnableCustomBinding = true)]
		public ActionResult BulkEditSelect(GridCommand command, BulkEditListModel model)
		{
			var gridModel = new GridModel<BulkEditProductModel>();

			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				var searchContext = new ProductSearchContext
				{
					StoreId = model.SearchStoreId,
					ManufacturerId = model.SearchManufacturerId,
					Keywords = model.SearchProductName,
					SearchSku = !_catalogSettings.SuppressSkuSearch,
					PageIndex = command.Page - 1,
					PageSize = command.PageSize,
					ShowHidden = true,
					ProductType = model.SearchProductTypeId > 0 ? (ProductType?)model.SearchProductTypeId : null
				};

				if (model.SearchCategoryId > 0)
					searchContext.CategoryIds.Add(model.SearchCategoryId);

				var products = _productService.SearchProducts(searchContext);

				gridModel.Data = products.Select(x =>
				{
					var productModel = new BulkEditProductModel
					{
						Id = x.Id,
						Name = x.Name,
						ProductTypeName = x.GetProductTypeLabel(_localizationService),
						ProductTypeLabelHint = x.ProductTypeLabelHint,
						Sku = x.Sku,
						OldPrice = x.OldPrice,
						Price = x.Price,
						ManageInventoryMethod = x.ManageInventoryMethod.GetLocalizedEnum(_localizationService, _workContext.WorkingLanguage.Id),
						StockQuantity = x.StockQuantity,
						Published = x.Published
					};

					return productModel;
				});

				gridModel.Total = products.TotalCount;
			}
			else
			{
				gridModel.Data = Enumerable.Empty<BulkEditProductModel>();

				NotifyAccessDenied();
			}

			return new JsonResult
			{
				Data = gridModel
			};
		}

		[AcceptVerbs(HttpVerbs.Post)]
		[HttpPost, GridAction(EnableCustomBinding = true)]
		public ActionResult BulkEditSave(GridCommand command,
			[Bind(Prefix = "updated")]IEnumerable<BulkEditProductModel> updatedProducts,
			[Bind(Prefix = "deleted")]IEnumerable<BulkEditProductModel> deletedProducts,
			BulkEditListModel model)
		{
			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				if (updatedProducts != null)
				{
					foreach (var pModel in updatedProducts)
					{
						var product = _productService.GetProductById(pModel.Id);
						if (product != null)
						{
							product.Sku = pModel.Sku;
							product.Price = pModel.Price;
							product.OldPrice = pModel.OldPrice;
							product.StockQuantity = pModel.StockQuantity;
							product.Published = pModel.Published;

							_productService.UpdateProduct(product);
						}
					}
				}

				if (deletedProducts != null)
				{
					foreach (var pModel in deletedProducts)
					{
						var product = _productService.GetProductById(pModel.Id);
						if (product != null)
						{
							_productService.DeleteProduct(product);
						}
					}
				}
			}

			return BulkEditSelect(command, model);
		}

		#endregion

		#region Tier prices

		[HttpPost, GridAction(EnableCustomBinding = true)]
		public ActionResult TierPriceList(GridCommand command, int productId)
		{
			var model = new GridModel<ProductModel.TierPriceModel>();

			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				var product = _productService.GetProductById(productId);

				var allStores = _services.StoreService.GetAllStores();
				var allCustomerRoles = _customerService.GetAllCustomerRoles(true);
				string allRolesString = T("Admin.Catalog.Products.TierPrices.Fields.CustomerRole.AllRoles");
				string allStoresString = T("Admin.Common.StoresAll");
				string deletedString = "[{0}]".FormatInvariant(T("Admin.Common.Deleted"));

				var tierPricesModel = product.TierPrices
					.OrderBy(x => x.StoreId)
					.ThenBy(x => x.Quantity)
					.ThenBy(x => x.CustomerRoleId)
					.Select(x =>
					{
						var tierPriceModel = new ProductModel.TierPriceModel
						{
							Id = x.Id,
							StoreId = x.StoreId,
							ProductId = x.ProductId,
							CustomerRoleId = x.CustomerRoleId ?? 0,
							Quantity = x.Quantity,
							Price1 = x.Price
						};

						if (x.CustomerRoleId.HasValue)
						{
							var role = allCustomerRoles.FirstOrDefault(r => r.Id == x.CustomerRoleId.Value);
							tierPriceModel.CustomerRole = (role == null ? allRolesString : role.Name);
						}
						else
						{
							tierPriceModel.CustomerRole = allRolesString;
						}

						if (x.StoreId > 0)
						{
							var store = allStores.FirstOrDefault(s => s.Id == x.StoreId);
							tierPriceModel.Store = (store == null ? deletedString : store.Name);
						}
						else
						{
							tierPriceModel.Store = allStoresString;
						}

						return tierPriceModel;
					})
					.ToList();

				model.Data = tierPricesModel;
				model.Total = tierPricesModel.Count;
			}
			else
			{
				model.Data = Enumerable.Empty<ProductModel.TierPriceModel>();

				NotifyAccessDenied();
			}

			return new JsonResult
			{
				Data = model
			};
		}

		[GridAction(EnableCustomBinding = true)]
		public ActionResult TierPriceInsert(GridCommand command, ProductModel.TierPriceModel model)
		{
			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				var product = _productService.GetProductById(model.ProductId);

				var tierPrice = new TierPrice
				{
					ProductId = model.ProductId,
					// use Store property (not Store propertyId) because appropriate property is stored in it
					StoreId = model.Store.ToInt(),
					// use CustomerRole property (not CustomerRoleId) because appropriate property is stored in it
					CustomerRoleId = model.CustomerRole.IsNumeric() && Int32.Parse(model.CustomerRole) != 0 ? Int32.Parse(model.CustomerRole) : (int?)null,
					Quantity = model.Quantity,
					Price = model.Price1
				};

				_productService.InsertTierPrice(tierPrice);

				//update "HasTierPrices" property
				_productService.UpdateHasTierPricesProperty(product);
			}

			return TierPriceList(command, model.ProductId);
		}

		[GridAction(EnableCustomBinding = true)]
		public ActionResult TierPriceUpdate(GridCommand command, ProductModel.TierPriceModel model)
		{
			var tierPrice = _productService.GetTierPriceById(model.Id);

			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				//use Store property (not Store propertyId) because appropriate property is stored in it
				tierPrice.StoreId = model.Store.ToInt();
				//use CustomerRole property (not CustomerRoleId) because appropriate property is stored in it
				tierPrice.CustomerRoleId = model.CustomerRole.IsNumeric() && Int32.Parse(model.CustomerRole) != 0 ? Int32.Parse(model.CustomerRole) : (int?)null;
				tierPrice.Quantity = model.Quantity;
				tierPrice.Price = model.Price1;

				_productService.UpdateTierPrice(tierPrice);
			}

			return TierPriceList(command, tierPrice.ProductId);
		}

		[GridAction(EnableCustomBinding = true)]
		public ActionResult TierPriceDelete(int id, GridCommand command)
		{
			var tierPrice = _productService.GetTierPriceById(id);
			var productId = tierPrice.ProductId;

			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				var product = _productService.GetProductById(productId);

				_productService.DeleteTierPrice(tierPrice);

				//update "HasTierPrices" property
				_productService.UpdateHasTierPricesProperty(product);
			}

			return TierPriceList(command, productId);
		}

		#endregion

		#region Product variant attributes

		[HttpPost, GridAction(EnableCustomBinding = true)]
		public ActionResult ProductVariantAttributeList(GridCommand command, int productId)
		{
			var model = new GridModel<ProductModel.ProductVariantAttributeModel>();

			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				var productVariantAttributes = _productAttributeService.GetProductVariantAttributesByProductId(productId);
				var productVariantAttributesModel = productVariantAttributes
					.Select(x =>
					{
						var pvaModel = new ProductModel.ProductVariantAttributeModel
						{
							Id = x.Id,
							ProductId = x.ProductId,
							ProductAttribute = _productAttributeService.GetProductAttributeById(x.ProductAttributeId).Name,
							ProductAttributeId = x.ProductAttributeId,
							TextPrompt = x.TextPrompt,
							IsRequired = x.IsRequired,
							AttributeControlType = x.AttributeControlType.GetLocalizedEnum(_localizationService, _workContext),
							AttributeControlTypeId = x.AttributeControlTypeId,
							DisplayOrder1 = x.DisplayOrder
						};

						if (x.ShouldHaveValues())
						{
							pvaModel.ViewEditUrl = Url.Action("EditAttributeValues", "Product", new { productVariantAttributeId = x.Id });
							pvaModel.ViewEditText = string.Format(_localizationService.GetResource("Admin.Catalog.Products.ProductVariantAttributes.Attributes.Values.ViewLink"), x.ProductVariantAttributeValues != null ? x.ProductVariantAttributeValues.Count : 0);
						}
						return pvaModel;
					})
					.ToList();

				model.Data = productVariantAttributesModel;
				model.Total = productVariantAttributesModel.Count;
			}
			else
			{
				model.Data = Enumerable.Empty<ProductModel.ProductVariantAttributeModel>();

				NotifyAccessDenied();
			}

			return new JsonResult
			{
				Data = model
			};
		}

		[GridAction(EnableCustomBinding = true)]
		public ActionResult ProductVariantAttributeInsert(GridCommand command, ProductModel.ProductVariantAttributeModel model)
		{
			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				var pva = new ProductVariantAttribute
				{
					ProductId = model.ProductId,
					ProductAttributeId = Int32.Parse(model.ProductAttribute), //use ProductAttribute property (not ProductAttributeId) because appropriate property is stored in it
					TextPrompt = model.TextPrompt,
					IsRequired = model.IsRequired,
					AttributeControlTypeId = Int32.Parse(model.AttributeControlType), //use AttributeControlType property (not AttributeControlTypeId) because appropriate property is stored in it
					DisplayOrder = model.DisplayOrder1
				};

				_productAttributeService.InsertProductVariantAttribute(pva);
			}

			return ProductVariantAttributeList(command, model.ProductId);
		}

		[GridAction(EnableCustomBinding = true)]
		public ActionResult ProductVariantAttributeUpdate(GridCommand command, ProductModel.ProductVariantAttributeModel model)
		{
			var pva = _productAttributeService.GetProductVariantAttributeById(model.Id);

			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				//use ProductAttribute property (not ProductAttributeId) because appropriate property is stored in it
				pva.ProductAttributeId = Int32.Parse(model.ProductAttribute);
				pva.TextPrompt = model.TextPrompt;
				pva.IsRequired = model.IsRequired;
				//use AttributeControlType property (not AttributeControlTypeId) because appropriate property is stored in it
				pva.AttributeControlTypeId = Int32.Parse(model.AttributeControlType);
				pva.DisplayOrder = model.DisplayOrder1;

				_productAttributeService.UpdateProductVariantAttribute(pva);
			}

			return ProductVariantAttributeList(command, pva.ProductId);
		}

		[GridAction(EnableCustomBinding = true)]
		public ActionResult ProductVariantAttributeDelete(int id, GridCommand command)
		{
			var pva = _productAttributeService.GetProductVariantAttributeById(id);
			var productId = pva.ProductId;

			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{	
				_productAttributeService.DeleteProductVariantAttribute(pva);
			}

			return ProductVariantAttributeList(command, productId);
		}

		public ActionResult AllProductVariantAttributes(string label, int selectedId)
		{
			var attributes = _productAttributeService.GetAllProductAttributes();

			if (label.HasValue())
			{
				attributes.Insert(0, new ProductAttribute { Name = label, Id = 0 });
			}

			var query = 
				from attr in attributes
				select new
				{
					id = attr.Id.ToString(),
					text = attr.Name,
					selected = attr.Id == selectedId
				};

			return new JsonResult { Data = query.ToList(), JsonRequestBehavior = JsonRequestBehavior.AllowGet };
		}

		#endregion

		#region Product variant attribute values

		public ActionResult EditAttributeValues(int productVariantAttributeId)
		{
			if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
				return AccessDeniedView();

			var pva = _productAttributeService.GetProductVariantAttributeById(productVariantAttributeId);
			if (pva == null)
				throw new ArgumentException("No product variant attribute found with the specified id");

			var product = _productService.GetProductById(pva.ProductId);
			if (product == null)
				throw new ArgumentException("No product found with the specified id");

			var model = new ProductModel.ProductVariantAttributeValueListModel()
			{
				ProductName = product.Name,
				ProductId = pva.ProductId,
				ProductVariantAttributeName = pva.ProductAttribute.Name,
				ProductVariantAttributeId = pva.Id,
			};

			return View(model);
		}

		[HttpPost, GridAction(EnableCustomBinding = true)]
		public ActionResult ProductAttributeValueList(int productVariantAttributeId, GridCommand command)
		{
			var gridModel = new GridModel<ProductModel.ProductVariantAttributeValueModel>();

			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				var pva = _productAttributeService.GetProductVariantAttributeById(productVariantAttributeId);

				var values = _productAttributeService.GetProductVariantAttributeValues(productVariantAttributeId);

				gridModel.Data = values.Select(x =>
				{
					var linkedProduct = _productService.GetProductById(x.LinkedProductId);

					var model = new ProductModel.ProductVariantAttributeValueModel
					{
						Id = x.Id,
						ProductVariantAttributeId = x.ProductVariantAttributeId,
						Name = x.ProductVariantAttribute.AttributeControlType != AttributeControlType.ColorSquares ? x.Name : string.Format("{0} - {1}", x.Name, x.ColorSquaresRgb),
						Alias = x.Alias,
						ColorSquaresRgb = x.ColorSquaresRgb,
						PriceAdjustment = x.PriceAdjustment,
						PriceAdjustmentString = (x.ValueType == ProductVariantAttributeValueType.Simple ? x.PriceAdjustment.ToString("G29") : ""),
						WeightAdjustment = x.WeightAdjustment,
						WeightAdjustmentString = (x.ValueType == ProductVariantAttributeValueType.Simple ? x.WeightAdjustment.ToString("G29") : ""),
						IsPreSelected = x.IsPreSelected,
						DisplayOrder = x.DisplayOrder,
						ValueTypeId = x.ValueTypeId,
						TypeName = x.ValueType.GetLocalizedEnum(_localizationService, _workContext),
						TypeNameClass = (x.ValueType == ProductVariantAttributeValueType.ProductLinkage ? "fa fa-link mr8" : "hide"),
						LinkedProductId = x.LinkedProductId,
						Quantity = x.Quantity
					};

					if (linkedProduct != null)
					{
						model.LinkedProductName = linkedProduct.GetLocalized(p => p.Name);
						model.LinkedProductTypeName = linkedProduct.GetProductTypeLabel(_localizationService);
						model.LinkedProductTypeLabelHint = linkedProduct.ProductTypeLabelHint;

						if (model.Quantity > 1)
							model.QuantityInfo = " × {0}".FormatWith(model.Quantity);
					}

					return model;
				});

				gridModel.Total = values.Count();
			}
			else
			{
				gridModel.Data = Enumerable.Empty<ProductModel.ProductVariantAttributeValueModel>();

				NotifyAccessDenied();
			}

			return new JsonResult
			{
				Data = gridModel
			};
		}

		public ActionResult ProductAttributeValueCreatePopup(int productAttributeAttributeId)
		{
			if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
				return AccessDeniedView();

			var pva = _productAttributeService.GetProductVariantAttributeById(productAttributeAttributeId);
			if (pva == null)
				throw new ArgumentException("No product variant attribute found with the specified id");

			var model = new ProductModel.ProductVariantAttributeValueModel()
			{
				ProductVariantAttributeId = productAttributeAttributeId,
				DisplayColorSquaresRgb = pva.AttributeControlType == AttributeControlType.ColorSquares,
				ColorSquaresRgb = "#000000",
				Quantity = 1
			};

			//locales
			AddLocales(_languageService, model.Locales);
			return View(model);
		}

		[HttpPost]
		public ActionResult ProductAttributeValueCreatePopup(string btnId, string formId, ProductModel.ProductVariantAttributeValueModel model)
		{
			if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
				return AccessDeniedView();

			var pva = _productAttributeService.GetProductVariantAttributeById(model.ProductVariantAttributeId);
			if (pva == null)
				return RedirectToAction("List", "Product");

			if (pva.AttributeControlType == AttributeControlType.ColorSquares)
			{
				//ensure valid color is chosen/entered
				if (String.IsNullOrEmpty(model.ColorSquaresRgb))
					ModelState.AddModelError("", "Color is required");
				try
				{
					var color = System.Drawing.ColorTranslator.FromHtml(model.ColorSquaresRgb);
				}
				catch (Exception exc)
				{
					ModelState.AddModelError("", exc.Message);
				}
			}

			if (ModelState.IsValid)
			{
				var pvav = new ProductVariantAttributeValue()
				{
					ProductVariantAttributeId = model.ProductVariantAttributeId,
					Name = model.Name,
					Alias = model.Alias,
					ColorSquaresRgb = model.ColorSquaresRgb,
					PriceAdjustment = model.PriceAdjustment,
					WeightAdjustment = model.WeightAdjustment,
					IsPreSelected = model.IsPreSelected,
					DisplayOrder = model.DisplayOrder,
					ValueTypeId = model.ValueTypeId,
					LinkedProductId = model.LinkedProductId,
					Quantity = model.Quantity
				};

				_productAttributeService.InsertProductVariantAttributeValue(pvav);
				UpdateLocales(pvav, model);

				ViewBag.RefreshPage = true;
				ViewBag.btnId = btnId;
				ViewBag.formId = formId;
				return View(model);
			}

			//If we got this far, something failed, redisplay form
			return View(model);
		}

		public ActionResult ProductAttributeValueEditPopup(int id)
		{
			if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
				return AccessDeniedView();

			var pvav = _productAttributeService.GetProductVariantAttributeValueById(id);
			if (pvav == null)
				return RedirectToAction("List", "Product");

			var linkedProduct = _productService.GetProductById(pvav.LinkedProductId);

			var model = new ProductModel.ProductVariantAttributeValueModel()
			{
				ProductVariantAttributeId = pvav.ProductVariantAttributeId,
				Name = pvav.Name,
				Alias = pvav.Alias,
				ColorSquaresRgb = pvav.ColorSquaresRgb,
				DisplayColorSquaresRgb = pvav.ProductVariantAttribute.AttributeControlType == AttributeControlType.ColorSquares,
				PriceAdjustment = pvav.PriceAdjustment,
				WeightAdjustment = pvav.WeightAdjustment,
				IsPreSelected = pvav.IsPreSelected,
				DisplayOrder = pvav.DisplayOrder,
				ValueTypeId = pvav.ValueTypeId,
				TypeName = pvav.ValueType.GetLocalizedEnum(_localizationService, _workContext),
				TypeNameClass = (pvav.ValueType == ProductVariantAttributeValueType.ProductLinkage ? "fa fa-link mr8" : "hide"),
				LinkedProductId = pvav.LinkedProductId,
				Quantity = pvav.Quantity
			};

			if (linkedProduct != null)
			{
				model.LinkedProductName = linkedProduct.GetLocalized(p => p.Name);
				model.LinkedProductTypeName = linkedProduct.GetProductTypeLabel(_localizationService);
				model.LinkedProductTypeLabelHint = linkedProduct.ProductTypeLabelHint;

				if (model.Quantity > 1)
					model.QuantityInfo = " × {0}".FormatWith(model.Quantity);
			}

			//locales
			AddLocales(_languageService, model.Locales, (locale, languageId) =>
			{
				locale.Name = pvav.GetLocalized(x => x.Name, languageId, false, false);
			});

			return View(model);
		}

		[HttpPost]
		public ActionResult ProductAttributeValueEditPopup(string btnId, string formId, ProductModel.ProductVariantAttributeValueModel model)
		{
			if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
				return AccessDeniedView();

			var pvav = _productAttributeService.GetProductVariantAttributeValueById(model.Id);
			if (pvav == null)
				return RedirectToAction("List", "Product");

			if (pvav.ProductVariantAttribute.AttributeControlType == AttributeControlType.ColorSquares)
			{
				//ensure valid color is chosen/entered
				if (String.IsNullOrEmpty(model.ColorSquaresRgb))
					ModelState.AddModelError("", "Color is required");
				try
				{
					var color = System.Drawing.ColorTranslator.FromHtml(model.ColorSquaresRgb);
				}
				catch (Exception exc)
				{
					ModelState.AddModelError("", exc.Message);
				}
			}

			if (ModelState.IsValid)
			{
				pvav.Name = model.Name;
				pvav.Alias = model.Alias;
				pvav.ColorSquaresRgb = model.ColorSquaresRgb;
				pvav.PriceAdjustment = model.PriceAdjustment;
				pvav.WeightAdjustment = model.WeightAdjustment;
				pvav.IsPreSelected = model.IsPreSelected;
				pvav.DisplayOrder = model.DisplayOrder;
				pvav.ValueTypeId = model.ValueTypeId;
				pvav.Quantity = model.Quantity;

				if (pvav.ValueType == ProductVariantAttributeValueType.Simple)
					pvav.LinkedProductId = 0;
				else
					pvav.LinkedProductId = model.LinkedProductId;

				_productAttributeService.UpdateProductVariantAttributeValue(pvav);

				UpdateLocales(pvav, model);

				ViewBag.RefreshPage = true;
				ViewBag.btnId = btnId;
				ViewBag.formId = formId;
				return View(model);
			}

			//If we got this far, something failed, redisplay form
			return View(model);
		}

		[GridAction(EnableCustomBinding = true)]
		public ActionResult ProductAttributeValueDelete(int pvavId, int productVariantAttributeId, GridCommand command)
		{
			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				var pvav = _productAttributeService.GetProductVariantAttributeValueById(pvavId);

				_productAttributeService.DeleteProductVariantAttributeValue(pvav);
			}

			return ProductAttributeValueList(productVariantAttributeId, command);
		}

		public ActionResult ProductAttributeValueLinkagePopup()
		{
			if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
				return AccessDeniedView();

			var model = new ProductModel.ProductVariantAttributeValueModel.AddProductLinkageModel();

			var allCategories = _categoryService.GetAllCategories(showHidden: true);
			var mappedCategories = allCategories.ToDictionary(x => x.Id);
			foreach (var c in allCategories)
			{
				model.AvailableCategories.Add(new SelectListItem { Text = c.GetCategoryNameWithPrefix(_categoryService, mappedCategories), Value = c.Id.ToString() });
			}

			foreach (var m in _manufacturerService.GetAllManufacturers(true))
			{
				model.AvailableManufacturers.Add(new SelectListItem { Text = m.Name, Value = m.Id.ToString() });
			}

			foreach (var s in _storeService.GetAllStores())
			{
				model.AvailableStores.Add(new SelectListItem { Text = s.Name, Value = s.Id.ToString() });
			}

			model.AvailableProductTypes = ProductType.SimpleProduct.ToSelectList(false).ToList();

			return View(model);
		}

		[HttpPost, GridAction(EnableCustomBinding = true)]
		public ActionResult ProductAttributeValueLinkagePopupList(GridCommand command, ProductModel.ProductVariantAttributeValueModel.AddProductLinkageModel model)
		{
			var gridModel = new GridModel<ProductModel>();

			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				var searchContext = new ProductSearchContext
				{
					CategoryIds = new List<int> { model.SearchCategoryId },
					ManufacturerId = model.SearchManufacturerId,
					StoreId = model.SearchStoreId,
					Keywords = model.SearchProductName,
					SearchSku = !_catalogSettings.SuppressSkuSearch,
					PageIndex = command.Page - 1,
					PageSize = command.PageSize,
					ShowHidden = true,
					ProductType = model.SearchProductTypeId > 0 ? (ProductType?)model.SearchProductTypeId : null
				};

				var products = _productService.SearchProducts(searchContext);

				gridModel.Data = products.Select(x =>
				{
                    var productModel = new ProductModel
                    {
                        Sku = x.Sku,
                        Published = x.Published,
                        ProductTypeLabelHint = x.ProductTypeLabelHint,
                        Name = x.Name,
                        Id = x.Id
                    };

					productModel.ProductTypeName = x.GetProductTypeLabel(_localizationService);

					return productModel;
				});

				gridModel.Total = products.TotalCount;
			}
			else
			{
				gridModel.Data = Enumerable.Empty<ProductModel>();

				NotifyAccessDenied();
			}

			return new JsonResult
			{
				Data = gridModel
			};
		}

		[HttpPost]
		[FormValueRequired("save")]
		public ActionResult ProductAttributeValueLinkagePopup(ProductModel.ProductVariantAttributeValueModel.AddProductLinkageModel model, string openerProductId, string openerProductName)
		{
			if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
				return AccessDeniedView();

			ViewBag.RefreshPage = true;
			ViewBag.openerProductId = openerProductId;
			ViewBag.openerProductName = openerProductName;

			var linkedProduct = _productService.GetProductById(model.ProductId);

			if (linkedProduct == null)
			{
				ViewBag.productId = 0;
				ViewBag.productName = "";
			}
			else
			{
				ViewBag.productId = linkedProduct.Id;
				ViewBag.productName = linkedProduct.Name;
			}
			return View(model);
		}

		#endregion

		#region Product variant attribute combinations

		private void PrepareProductAttributeCombinationModel(
			ProductVariantAttributeCombinationModel model, 
			ProductVariantAttributeCombination entity,
			Product product, bool formatAttributes = false)
		{
			if (model == null)
				throw new ArgumentNullException("model");
			
			if (product == null)
				throw new ArgumentNullException("variant");

			model.ProductId = product.Id;

			if (entity == null)
			{
				// is a new entity, so initialize it properly
				model.StockQuantity = 10000;
				model.IsActive = true;
				model.AllowOutOfStockOrders = true;
			}

			if (formatAttributes && entity != null)
			{
				model.AttributesXml = _productAttributeFormatter.FormatAttributes(product, entity.AttributesXml, _workContext.CurrentCustomer, "<br />", true, true, true, false);
			}
		}
		private void PrepareVariantCombinationAttributes(ProductVariantAttributeCombinationModel model, Product product)
		{
			var productVariantAttributes = _productAttributeService.GetProductVariantAttributesByProductId(product.Id);
			foreach (var attribute in productVariantAttributes)
			{
				var pvaModel = new ProductVariantAttributeCombinationModel.ProductVariantAttributeModel()
				{
					Id = attribute.Id,
					ProductAttributeId = attribute.ProductAttributeId,
					Name = attribute.ProductAttribute.Name,
					TextPrompt = attribute.TextPrompt,
					IsRequired = attribute.IsRequired,
					AttributeControlType = attribute.AttributeControlType
				};

				if (attribute.ShouldHaveValues())
				{
					//values
					var pvaValues = _productAttributeService.GetProductVariantAttributeValues(attribute.Id);
					foreach (var pvaValue in pvaValues)
					{
						var pvaValueModel = new ProductVariantAttributeCombinationModel.ProductVariantAttributeValueModel()
						{
							Id = pvaValue.Id,
							Name = pvaValue.Name,
							IsPreSelected = pvaValue.IsPreSelected
						};
						pvaModel.Values.Add(pvaValueModel);
					}
				}

				model.ProductVariantAttributes.Add(pvaModel);
			}
		}
		private void PrepareVariantCombinationPictures(ProductVariantAttributeCombinationModel model, Product product)
		{
			var pictures = _pictureService.GetPicturesByProductId(product.Id);
			foreach (var picture in pictures)
			{
				var assignablePicture = new ProductVariantAttributeCombinationModel.PictureSelectItemModel();
				assignablePicture.Id = picture.Id;
				assignablePicture.IsAssigned = model.AssignedPictureIds.Contains(picture.Id);
				assignablePicture.PictureUrl = _pictureService.GetPictureUrl(picture.Id, 125, false);
				model.AssignablePictures.Add(assignablePicture);
			}
		}
		private void PrepareViewBag(string btnId, string formId, bool refreshPage = false, bool isEdit = true)
		{
			ViewBag.btnId = btnId;
			ViewBag.formId = formId;
			ViewBag.RefreshPage = refreshPage;
			ViewBag.IsEdit = isEdit;
		}
		private void PrepareDeliveryTimes(ProductVariantAttributeCombinationModel model, int? selectId = null)
		{
			var deliveryTimes = _deliveryTimesService.GetAllDeliveryTimes();

			foreach (var dt in deliveryTimes)
			{
				model.AvailableDeliveryTimes.Add(new SelectListItem()
				{
					Text = dt.Name,
					Value = dt.Id.ToString(),
					Selected = (selectId == dt.Id)
				});
			}
		}

		[HttpPost, GridAction(EnableCustomBinding = true)]
		public ActionResult ProductVariantAttributeCombinationList(GridCommand command, int productId)
		{
			var model = new GridModel<ProductVariantAttributeCombinationModel>();

			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				// TODO: Replace ProductModel.ProductVariantAttributeCombinationModel by AddProductVariantAttributeCombinationModel
				// when there's no grid-inline-editing anymore.

				var product = _productService.GetProductById(productId);

				var allCombinations = _productAttributeService.GetAllProductVariantAttributeCombinations(product.Id, command.Page - 1, command.PageSize);

				var productUrlTitle = _localizationService.GetResource("Common.OpenInShop");
				var productSeName = product.GetSeName();

				var productVariantAttributesModel = allCombinations.Select(x =>
				{
					var pvacModel = x.ToModel();
					PrepareProductAttributeCombinationModel(pvacModel, x, product, true);

					pvacModel.ProductUrl = _productAttributeParser.GetProductUrlWithAttributes(x.AttributesXml, product.Id, productSeName);
					pvacModel.ProductUrlTitle = productUrlTitle;

					try
					{
						var firstAttribute = _productAttributeParser.DeserializeProductVariantAttributes(x.AttributesXml).FirstOrDefault();

						var attribute = x.Product.ProductVariantAttributes.FirstOrDefault(y => y.Id == firstAttribute.Key);
						var attributeValue = attribute.ProductVariantAttributeValues.FirstOrDefault(y => y.Id == int.Parse(firstAttribute.Value.First()));

						pvacModel.DisplayOrder = attributeValue.DisplayOrder;
					}
					catch (Exception exc)
					{
						exc.Dump();
					}

					//if (x.IsDefaultCombination)
					//	pvacModel.AttributesXml = "<b>{0}</b>".FormatWith(pvacModel.AttributesXml);

					//warnings
					var warnings = _shoppingCartService.GetShoppingCartItemAttributeWarnings(
							_workContext.CurrentCustomer,
							ShoppingCartType.ShoppingCart,
							x.Product,
							x.AttributesXml,
							combination: x);

					pvacModel.Warnings.AddRange(warnings);

					return pvacModel;
				})
				.OrderBy(x => x.DisplayOrder)
				.ToList();

				model.Data = productVariantAttributesModel;
				model.Total = allCombinations.TotalCount;
			}
			else
			{
				model.Data = Enumerable.Empty<ProductVariantAttributeCombinationModel>();

				NotifyAccessDenied();
			}

			return new JsonResult
			{
				Data = model
			};
		}

		[GridAction(EnableCustomBinding = true)]
		public ActionResult ProductVariantAttributeCombinationDelete(int id, GridCommand command)
		{
			var pvac = _productAttributeService.GetProductVariantAttributeCombinationById(id);
			var productId = pvac.ProductId;

			if (_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
			{
				_productAttributeService.DeleteProductVariantAttributeCombination(pvac);

				var product = _productService.GetProductById(productId);
				_productService.UpdateLowestAttributeCombinationPriceProperty(product);
			}

			return ProductVariantAttributeCombinationList(command, productId);
		}

		public ActionResult AttributeCombinationCreatePopup(string btnId, string formId, int productId)
		{
			if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
				return AccessDeniedView();

			var product = _productService.GetProductById(productId);
			if (product == null)
				return RedirectToAction("List", "Product");

			var model = new ProductVariantAttributeCombinationModel();

			PrepareProductAttributeCombinationModel(model, null, product);
			PrepareVariantCombinationAttributes(model, product);
			PrepareVariantCombinationPictures(model, product);
			PrepareDeliveryTimes(model);
			PrepareViewBag(btnId, formId, false, false);

			return View(model);
		}

		[HttpPost]
		[ValidateInput(false)]
		public ActionResult AttributeCombinationCreatePopup(string btnId, string formId, int productId, ProductVariantAttributeCombinationModel model, FormCollection form)
		{
			if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
				return AccessDeniedView();

			var product = _productService.GetProductById(productId);
			if (product == null)
				return RedirectToAction("List", "Product");

			var warnings = new List<string>();
			var variantAttributes = _productAttributeService.GetProductVariantAttributesByProductId(product.Id);

			string attributeXml = form.CreateSelectedAttributesXml(
				product.Id, 
				variantAttributes, 
				_productAttributeParser, 
				_localizationService,
				_downloadService, 
				_catalogSettings, 
				this.Request, 
				warnings, 
				false);

			warnings.AddRange(_shoppingCartService.GetShoppingCartItemAttributeWarnings(_workContext.CurrentCustomer, ShoppingCartType.ShoppingCart, product, attributeXml));

			if (_productAttributeParser.FindProductVariantAttributeCombination(product.Id, attributeXml) != null)
			{
				warnings.Add(_localizationService.GetResource("Admin.Catalog.Products.ProductVariantAttributes.AttributeCombinations.CombiExists"));
			}

			if (warnings.Count == 0)
			{
				var combination = model.ToEntity();
				combination.AttributesXml = attributeXml;
				combination.SetAssignedPictureIds(model.AssignedPictureIds);

				_productAttributeService.InsertProductVariantAttributeCombination(combination);

				_productService.UpdateLowestAttributeCombinationPriceProperty(product);
			}

			PrepareProductAttributeCombinationModel(model, null, product);
			PrepareVariantCombinationAttributes(model, product);
			PrepareVariantCombinationPictures(model, product);
			PrepareDeliveryTimes(model);
			PrepareViewBag(btnId, formId, warnings.Count == 0, false);

			if (warnings.Count > 0)
				model.Warnings = warnings;

			return View(model);
		}

		public ActionResult AttributeCombinationEditPopup(int id, string btnId, string formId)
		{
			if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
				return AccessDeniedView();

			var combination = _productAttributeService.GetProductVariantAttributeCombinationById(id);
			if (combination == null)
			{
				return RedirectToAction("List", "Product");
			}

			var product = _productService.GetProductById(combination.ProductId);
			if (product == null)
				return RedirectToAction("List", "Product");

			var model = combination.ToModel();

			PrepareProductAttributeCombinationModel(model, combination, product, true);
			PrepareVariantCombinationAttributes(model, product);
			PrepareVariantCombinationPictures(model, product);
			PrepareDeliveryTimes(model, model.DeliveryTimeId);
			PrepareViewBag(btnId, formId);

			return View(model);
		}

		[HttpPost]
		[ValidateInput(false)]
		public ActionResult AttributeCombinationEditPopup(string btnId, string formId, ProductVariantAttributeCombinationModel model, FormCollection form)
		{
			if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
				return AccessDeniedView();

			if (ModelState.IsValid)
			{
				var combination = _productAttributeService.GetProductVariantAttributeCombinationById(model.Id);
				if (combination == null)
					return RedirectToAction("List", "Product");

				string attributeXml = combination.AttributesXml;
				combination = model.ToEntity(combination);
				combination.AttributesXml = attributeXml;
				combination.SetAssignedPictureIds(model.AssignedPictureIds);

				_productAttributeService.UpdateProductVariantAttributeCombination(combination);

				_productService.UpdateLowestAttributeCombinationPriceProperty(combination.Product);

				PrepareViewBag(btnId, formId, true);
			}
			return View(model);
		}

		[HttpPost]
		[ValidateInput(false)]
		public ActionResult CreateAllAttributeCombinations(ProductVariantAttributeCombinationModel model, int productId)
		{
			if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
				return AccessDeniedView();

			var product = _productService.GetProductById(productId);
			if (product == null)
				throw new ArgumentException("No product found with the specified id");

			_productAttributeService.CreateAllProductVariantAttributeCombinations(product);

			_productService.UpdateLowestAttributeCombinationPriceProperty(product);

			return new JsonResult { Data = "" };
		}

		[HttpPost]
		[ValidateInput(false)]
		public ActionResult DeleteAllAttributeCombinations(ProductVariantAttributeCombinationModel model, int productId)
		{
			if (!_permissionService.Authorize(StandardPermissionProvider.ManageCatalog))
				return AccessDeniedView();

			var product = _productService.GetProductById(productId);
			if (product == null)
				throw new ArgumentException("No product found with the specified id");

			_pvacRepository.DeleteAll(x => x.ProductId == product.Id);

			_productService.UpdateLowestAttributeCombinationPriceProperty(product);

			return new JsonResult { Data = "" };
		}

		[HttpPost]
		public ActionResult CombinationExistenceNote(int productId, FormCollection form)
		{
			// no further authorization here

			var warnings = new List<string>();
			var attributes = _productAttributeService.GetProductVariantAttributesByProductId(productId);

			string attributeXml = form.CreateSelectedAttributesXml(productId, attributes, _productAttributeParser,
				_localizationService, _downloadService, _catalogSettings, this.Request, warnings, false);

			bool exists = (_productAttributeParser.FindProductVariantAttributeCombination(productId, attributeXml) != null);

			if (!exists)
			{
				var product = _productService.GetProductById(productId);
				if (product != null)
					warnings.AddRange(_shoppingCartService.GetShoppingCartItemAttributeWarnings(_workContext.CurrentCustomer, ShoppingCartType.ShoppingCart, product, attributeXml));
			}

			if (warnings.Count > 0)
			{
				return new JsonResult
				{
					Data = new
					{
						Message = warnings[0],
						HasWarning = true
					}
				};
			}

			return new JsonResult
			{
				Data = new
				{
					Message = _localizationService.GetResource(exists ?
						"Admin.Catalog.Products.ProductVariantAttributes.AttributeCombinations.CombiExists" :
						"Admin.Catalog.Products.Variants.ProductVariantAttributes.AttributeCombinations.CombiNotExists"
					),
					HasWarning = exists
				}
			};
		}

		#endregion

		#endregion
	}
}
