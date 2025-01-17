﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;
using SmartStore.Core;
using SmartStore.Core.Data;
using SmartStore.Core.Domain.Catalog;
using SmartStore.Core.Domain.Common;
using SmartStore.Core.Domain.Customers;
using SmartStore.Core.Domain.DataExchange;
using SmartStore.Core.Domain.Media;
using SmartStore.Core.Domain.Messages;
using SmartStore.Core.Domain.Orders;
using SmartStore.Core.Domain.Stores;
using SmartStore.Core.Email;
using SmartStore.Core.Localization;
using SmartStore.Core.Logging;
using SmartStore.Services.Catalog;
using SmartStore.Services.Common;
using SmartStore.Services.Customers;
using SmartStore.Services.DataExchange.Export.Deployment;
using SmartStore.Services.DataExchange.Export.Internal;
using SmartStore.Services.Directory;
using SmartStore.Services.Helpers;
using SmartStore.Services.Localization;
using SmartStore.Services.Media;
using SmartStore.Services.Messages;
using SmartStore.Services.Orders;
using SmartStore.Services.Security;
using SmartStore.Services.Seo;
using SmartStore.Services.Shipping;
using SmartStore.Services.Tax;
using SmartStore.Utilities;
using SmartStore.Utilities.Threading;

namespace SmartStore.Services.DataExchange.Export
{
	public partial class DataExporter : IDataExporter
	{
		private static readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();

		#region Dependencies

		private readonly ICommonServices _services;
		private readonly IDbContext _dbContext;
		private readonly Lazy<IPriceFormatter> _priceFormatter;
		private readonly Lazy<IDateTimeHelper> _dateTimeHelper;
		private readonly Lazy<IExportProfileService> _exportProfileService;
        private readonly Lazy<ILocalizedEntityService> _localizedEntityService;
		private readonly Lazy<ILanguageService> _languageService;
        private readonly Lazy<IUrlRecordService> _urlRecordService;
		private readonly Lazy<IPictureService> _pictureService;
		private readonly Lazy<IPriceCalculationService> _priceCalculationService;
		private readonly Lazy<ICurrencyService> _currencyService;
		private readonly Lazy<ITaxService> _taxService;
		private readonly Lazy<ICategoryService> _categoryService;
		private readonly Lazy<IProductAttributeParser> _productAttributeParser;
		private readonly Lazy<IProductAttributeService> _productAttributeService;
		private readonly Lazy<IProductTemplateService> _productTemplateService;
        private readonly Lazy<IProductService> _productService;
		private readonly Lazy<IOrderService> _orderService;
		private readonly Lazy<IManufacturerService> _manufacturerService;
		private readonly ICustomerService _customerService;
		private readonly Lazy<IAddressService> _addressService;
		private readonly Lazy<ICountryService> _countryService;
        private readonly Lazy<IShipmentService> _shipmentService;
		private readonly Lazy<IGenericAttributeService> _genericAttributeService;
		private readonly Lazy<IEmailAccountService> _emailAccountService;
		private readonly Lazy<IQueuedEmailService> _queuedEmailService;
		private readonly Lazy<IEmailSender> _emailSender;
		private readonly Lazy<IDeliveryTimeService> _deliveryTimeService;
		private readonly Lazy<IQuantityUnitService> _quantityUnitService;

		private readonly Lazy<IRepository<Customer>>_customerRepository;
		private readonly Lazy<IRepository<NewsLetterSubscription>> _subscriptionRepository;
		private readonly Lazy<IRepository<Order>> _orderRepository;

		private readonly Lazy<DataExchangeSettings> _dataExchangeSettings;
		private readonly Lazy<MediaSettings> _mediaSettings;
		private readonly Lazy<ContactDataSettings> _contactDataSettings;

		public DataExporter(
			ICommonServices services,
			IDbContext dbContext,
            Lazy<IPriceFormatter> priceFormatter,
			Lazy<IDateTimeHelper> dateTimeHelper,
			Lazy<IExportProfileService> exportProfileService,
			Lazy<ILocalizedEntityService> localizedEntityService,
			Lazy<ILanguageService> languageService,
			Lazy<IUrlRecordService> urlRecordService,
			Lazy<IPictureService> pictureService,
			Lazy<IPriceCalculationService> priceCalculationService,
			Lazy<ICurrencyService> currencyService,
			Lazy<ITaxService> taxService,
			Lazy<ICategoryService> categoryService,
			Lazy<IProductAttributeParser> productAttributeParser,
			Lazy<IProductAttributeService> productAttributeService,
			Lazy<IProductTemplateService> productTemplateService,
			Lazy<IProductService> productService,
			Lazy<IOrderService> orderService,
			Lazy<IManufacturerService> manufacturerService,
			ICustomerService customerService,
			Lazy<IAddressService> addressService,
			Lazy<ICountryService> countryService,
			Lazy<IShipmentService> shipmentService,
			Lazy<IGenericAttributeService> genericAttributeService,
			Lazy<IEmailAccountService> emailAccountService,
			Lazy<IQueuedEmailService> queuedEmailService,
            Lazy<IEmailSender> emailSender,
			Lazy<IDeliveryTimeService> deliveryTimeService,
			Lazy<IQuantityUnitService> quantityUnitService,
            Lazy<IRepository<Customer>> customerRepository,
			Lazy<IRepository<NewsLetterSubscription>> subscriptionRepository,
			Lazy<IRepository<Order>> orderRepository,
            Lazy<DataExchangeSettings> dataExchangeSettings,
			Lazy<MediaSettings> mediaSettings,
			Lazy<ContactDataSettings> contactDataSettings)
		{
			_services = services;
			_dbContext = dbContext;
			_priceFormatter = priceFormatter;
			_dateTimeHelper = dateTimeHelper;
			_exportProfileService = exportProfileService;
			_localizedEntityService = localizedEntityService;
			_languageService = languageService;
			_urlRecordService = urlRecordService;
			_pictureService = pictureService;
			_priceCalculationService = priceCalculationService;
			_currencyService = currencyService;
			_taxService = taxService;
			_categoryService = categoryService;
			_productAttributeParser = productAttributeParser;
			_productAttributeService = productAttributeService;
			_productTemplateService = productTemplateService;
			_productService = productService;
			_orderService = orderService;
			_manufacturerService = manufacturerService;
			_customerService = customerService;
			_addressService = addressService;
			_countryService = countryService;
			_shipmentService = shipmentService;
			_genericAttributeService = genericAttributeService;
			_emailAccountService = emailAccountService;
			_queuedEmailService = queuedEmailService;
			_emailSender = emailSender;
			_deliveryTimeService = deliveryTimeService;
			_quantityUnitService = quantityUnitService;

			_customerRepository = customerRepository;
			_subscriptionRepository = subscriptionRepository;
			_orderRepository = orderRepository;

			_dataExchangeSettings = dataExchangeSettings;
			_mediaSettings = mediaSettings;
			_contactDataSettings = contactDataSettings;

			T = NullLocalizer.Instance;
		}

		public Localizer T { get; set; }

		#endregion

		#region Utilities

		private void SetProgress(DataExporterContext ctx, int loadedRecords)
		{
			try
			{
				if (!ctx.IsPreview && loadedRecords > 0)
				{
					int totalRecords = ctx.RecordsPerStore.Sum(x => x.Value);

					if (ctx.Request.Profile.Limit > 0 && totalRecords > ctx.Request.Profile.Limit)
						totalRecords = ctx.Request.Profile.Limit;

					ctx.RecordCount = Math.Min(ctx.RecordCount + loadedRecords, totalRecords);

					var msg = ctx.ProgressInfo.FormatInvariant(ctx.RecordCount, totalRecords);

					ctx.Request.ProgressValueSetter.Invoke(ctx.RecordCount, totalRecords, msg);
				}
			}
			catch { }
		}

		private void SetProgress(DataExporterContext ctx, string message)
		{
			try
			{
				if (!ctx.IsPreview && message.HasValue())
				{
					ctx.Request.ProgressValueSetter.Invoke(0, 0, message);
				}
			}
			catch { }
		}

		private bool HasPermission(DataExporterContext ctx)
		{
			if (ctx.Request.HasPermission)
				return true;

			if (ctx.Request.CustomerId == 0)
				ctx.Request.CustomerId = _services.WorkContext.CurrentCustomer.Id;	// fallback to background task system customer

			var customer = _customerService.GetCustomerById(ctx.Request.CustomerId);

			if (ctx.Request.Provider.Value.EntityType == ExportEntityType.Product ||
				ctx.Request.Provider.Value.EntityType == ExportEntityType.Category ||
				ctx.Request.Provider.Value.EntityType == ExportEntityType.Manufacturer)
				return _services.Permissions.Authorize(StandardPermissionProvider.ManageCatalog, customer);

			if (ctx.Request.Provider.Value.EntityType == ExportEntityType.Customer)
				return _services.Permissions.Authorize(StandardPermissionProvider.ManageCustomers, customer);

			if (ctx.Request.Provider.Value.EntityType == ExportEntityType.Order)
				return _services.Permissions.Authorize(StandardPermissionProvider.ManageOrders, customer);

			if (ctx.Request.Provider.Value.EntityType == ExportEntityType.NewsLetterSubscription)
				return _services.Permissions.Authorize(StandardPermissionProvider.ManageNewsletterSubscribers, customer);

			return true;
		}

		private void DetachAllEntitiesAndClear(DataExporterContext ctx)
		{
			try
			{
				_dbContext.DetachAll();
			}
			catch (Exception exception)
			{
				ctx.Log.Warning("Detaching all entities failed.", exception);
			}

			try
			{
				// now again attach what is globally required
				_dbContext.Attach(ctx.Request.Profile);
				_dbContext.AttachRange(ctx.Stores.Values);
            }
			catch (Exception exception)
			{
				ctx.Log.Warning("Re-attaching entities failed.", exception);
			}

			try
			{
				if (ctx.ProductExportContext != null)
					ctx.ProductExportContext.Clear();

				if (ctx.OrderExportContext != null)
					ctx.OrderExportContext.Clear();

				if (ctx.ManufacturerExportContext != null)
					ctx.ManufacturerExportContext.Clear();

				if (ctx.CategoryExportContext != null)
					ctx.CategoryExportContext.Clear();

				if (ctx.CustomerExportContext != null)
					ctx.CustomerExportContext.Clear();
			}
			catch {	}
		}

		private IExportDataSegmenterProvider CreateSegmenter(DataExporterContext ctx, int pageIndex = 0)
		{
			var offset = ctx.Request.Profile.Offset + (pageIndex * PageSize);

			var limit = (ctx.IsPreview ? PageSize : ctx.Request.Profile.Limit);

			var recordsPerSegment = (ctx.IsPreview ? 0 : ctx.Request.Profile.BatchSize);

			var totalCount = ctx.Request.Profile.Offset + ctx.RecordsPerStore.First(x => x.Key == ctx.Store.Id).Value;

			switch (ctx.Request.Provider.Value.EntityType)
			{
				case ExportEntityType.Product:
					ctx.ExecuteContext.Segmenter = new ExportDataSegmenter<Product>
					(
						skip => GetProducts(ctx, skip),
						entities =>
						{
							// load data behind navigation properties for current queue in one go
							ctx.ProductExportContext = new ProductExportContext(entities,
								x => _productAttributeService.Value.GetProductVariantAttributesByProductIds(x, null),
								x => _productAttributeService.Value.GetProductVariantAttributeCombinations(x),
								x => _productService.Value.GetTierPricesByProductIds(x, (ctx.Projection.CurrencyId ?? 0) != 0 ? ctx.ContextCustomer : null, ctx.Store.Id),
								x => _categoryService.Value.GetProductCategoriesByProductIds(x),
								x => _manufacturerService.Value.GetProductManufacturersByProductIds(x),
								x => _productService.Value.GetProductPicturesByProductIds(x),
								x => _productService.Value.GetProductTagsByProductIds(x),
								x => _productService.Value.GetAppliedDiscountsByProductIds(x),
								x => _productService.Value.GetProductSpecificationAttributesByProductIds(x),
								x => _productService.Value.GetBundleItemsByProductIds(x, true)
							);
						},
						entity => Convert(ctx, entity),
						offset, PageSize, limit, recordsPerSegment, totalCount
					);
					break;

				case ExportEntityType.Order:
					ctx.ExecuteContext.Segmenter = new ExportDataSegmenter<Order>
					(
						skip => GetOrders(ctx, skip),
						entities =>
						{
							ctx.OrderExportContext = new OrderExportContext(entities,
								x => _customerService.GetCustomersByIds(x),
								x => _customerService.GetRewardPointsHistoriesByCustomerIds(x),
								x => _addressService.Value.GetAddressByIds(x),
								x => _orderService.Value.GetOrderItemsByOrderIds(x),
								x => _shipmentService.Value.GetShipmentsByOrderIds(x)
							);
						},
						entity => Convert(ctx, entity),
						offset, PageSize, limit, recordsPerSegment, totalCount
					);
					break;

				case ExportEntityType.Manufacturer:
					ctx.ExecuteContext.Segmenter = new ExportDataSegmenter<Manufacturer>
					(
						skip => GetManufacturers(ctx, skip),
						entities =>
						{
							ctx.ManufacturerExportContext = new ManufacturerExportContext(entities,
								x => _manufacturerService.Value.GetProductManufacturersByManufacturerIds(x),
								x => _pictureService.Value.GetPicturesByIds(x)
							);
						},
						entity => Convert(ctx, entity),
						offset, PageSize, limit, recordsPerSegment, totalCount
					);
					break;

				case ExportEntityType.Category:
					ctx.ExecuteContext.Segmenter = new ExportDataSegmenter<Category>
					(
						skip => GetCategories(ctx, skip),
						entities =>
						{
							ctx.CategoryExportContext = new CategoryExportContext(entities,
								x => _categoryService.Value.GetProductCategoriesByCategoryIds(x),
								x => _pictureService.Value.GetPicturesByIds(x)
							);
						},
						entity => Convert(ctx, entity),
						offset, PageSize, limit, recordsPerSegment, totalCount
					);
					break;

				case ExportEntityType.Customer:
					ctx.ExecuteContext.Segmenter = new ExportDataSegmenter<Customer>
					(
						skip => GetCustomers(ctx, skip),
						entities =>
						{
							ctx.CustomerExportContext = new CustomerExportContext(entities,
								x => _genericAttributeService.Value.GetAttributesForEntity(x, "Customer")
							);
						},
						entity => Convert(ctx, entity),
						offset, PageSize, limit, recordsPerSegment, totalCount
					);
					break;

				case ExportEntityType.NewsLetterSubscription:
					ctx.ExecuteContext.Segmenter = new ExportDataSegmenter<NewsLetterSubscription>
					(
						skip => GetNewsLetterSubscriptions(ctx, skip),
						null,
						entity => Convert(ctx, entity),
						offset, PageSize, limit, recordsPerSegment, totalCount
					);
					break;

				default:
					ctx.ExecuteContext.Segmenter = null;
					break;
			}

			return ctx.ExecuteContext.Segmenter as IExportDataSegmenterProvider;
		}

		private bool CallProvider(DataExporterContext ctx, string streamId, string method, string path)
		{
			if (method != "Execute" && method != "OnExecuted")
				throw new SmartException("Unknown export method {0}.".FormatInvariant(method.NaIfEmpty()));

			try
			{
				ctx.ExecuteContext.DataStreamId = streamId;

				using (ctx.ExecuteContext.DataStream = new MemoryStream())
				{
					if (method == "Execute")
					{
						ctx.Request.Provider.Value.Execute(ctx.ExecuteContext);
					}
					else if (method == "OnExecuted")
					{
						ctx.Request.Provider.Value.OnExecuted(ctx.ExecuteContext);
					}

					if (ctx.IsFileBasedExport && path.HasValue() && ctx.ExecuteContext.DataStream.Length > 0)
					{
						if (!ctx.ExecuteContext.DataStream.CanSeek)
							ctx.Log.Warning("Data stream seems to be closed!");

						ctx.ExecuteContext.DataStream.Seek(0, SeekOrigin.Begin);

						using (_rwLock.GetWriteLock())
						using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
						{
							ctx.Log.Information("Creating file {0}.".FormatInvariant(path));

							ctx.ExecuteContext.DataStream.CopyTo(fileStream);
						}
					}
				}
			}
			catch (Exception exception)
			{
				ctx.ExecuteContext.Abort = DataExchangeAbortion.Hard;
				ctx.Log.Error("The provider failed at the {0} method: {1}.".FormatInvariant(method, exception.ToAllMessages()), exception);
				ctx.Result.LastError = exception.ToString();
			}
			finally
			{
				if (ctx.ExecuteContext.DataStream != null)
				{
					ctx.ExecuteContext.DataStream.Dispose();
					ctx.ExecuteContext.DataStream = null;
				}
			}

			return (ctx.ExecuteContext.Abort != DataExchangeAbortion.Hard);
		}

		private void Deploy(DataExporterContext ctx, string zipPath)
		{
			var allFiles = System.IO.Directory.GetFiles(ctx.FolderContent, "*.*", SearchOption.AllDirectories);

			var context = new ExportDeploymentContext
			{
				Log = ctx.Log,
				FolderContent = ctx.FolderContent,
				ZipPath = zipPath
			};

			foreach (var deployment in ctx.Request.Profile.Deployments.OrderBy(x => x.DeploymentTypeId).Where(x => x.Enabled))
			{
				if (deployment.CreateZip)
					context.DeploymentFiles = new string[] { zipPath };
				else
					context.DeploymentFiles = allFiles;

				try
				{
					IFilePublisher publisher = null;

					if (deployment.DeploymentType == ExportDeploymentType.Email)
					{
						publisher = new EmailFilePublisher(_emailAccountService.Value, _queuedEmailService.Value);
					}
					else if (deployment.DeploymentType == ExportDeploymentType.FileSystem)
					{
						publisher = new FileSystemFilePublisher();
					}
					else if (deployment.DeploymentType == ExportDeploymentType.Ftp)
					{
						publisher = new FtpFilePublisher();
					}
					else if (deployment.DeploymentType == ExportDeploymentType.Http)
					{
						publisher = new HttpFilePublisher();
					}

					if (publisher != null)
					{
						publisher.Publish(context, deployment);
					}
				}
				catch (Exception exception)
				{
					ctx.Log.Error("Deployment \"{0}\" of type {1} failed: {2}".FormatInvariant(
						deployment.Name, deployment.DeploymentType.ToString(), exception.Message), exception);
				}
			}
		}

		private void SendCompletionEmail(DataExporterContext ctx, string zipPath)
		{
			var	emailAccount = _emailAccountService.Value.GetEmailAccountById(ctx.Request.Profile.EmailAccountId);

			if (emailAccount == null)
				emailAccount = _emailAccountService.Value.GetDefaultEmailAccount();

			var downloadUrl = "{0}Admin/Export/DownloadExportFile/{1}?name=".FormatInvariant(_services.WebHelper.GetStoreLocation(false), ctx.Request.Profile.Id);

			var languageId = ctx.Projection.LanguageId ?? 0;
			var smtpContext = new SmtpContext(emailAccount);
			var message = new EmailMessage();

			var storeInfo = "{0} ({1})".FormatInvariant(ctx.Store.Name, ctx.Store.Url);
			var intro =_services.Localization.GetResource("Admin.DataExchange.Export.CompletedEmail.Body", languageId).FormatInvariant(storeInfo);
			var body = new StringBuilder(intro);

			if (ctx.Result.LastError.HasValue())
			{
				body.AppendFormat("<p>{0}</p>", ctx.Result.LastError);
			}

			if (ctx.IsFileBasedExport && File.Exists(zipPath))
			{
				var fileName = Path.GetFileName(zipPath);
				body.AppendFormat("<p><a href='{0}' download>{1}</a></p>", downloadUrl + HttpUtility.UrlDecode(fileName), fileName);
			}

			if (ctx.IsFileBasedExport && ctx.Result.Files.Count > 0)
			{
				body.Append("<p>");
				foreach (var file in ctx.Result.Files)
				{
					body.AppendFormat("<div><a href='{0}' download>{1}</a></div>", downloadUrl + HttpUtility.UrlDecode(file.FileName), file.FileName);
				}
				body.Append("</p>");
			}

			message.From = new EmailAddress(emailAccount.Email, emailAccount.DisplayName);

			if (ctx.Request.Profile.CompletedEmailAddresses.HasValue())
				message.To.AddRange(ctx.Request.Profile.CompletedEmailAddresses.SplitSafe(",").Where(x => x.IsEmail()).Select(x => new EmailAddress(x)));

			if (message.To.Count == 0 && _contactDataSettings.Value.WebmasterEmailAddress.HasValue())
				message.To.Add(new EmailAddress(_contactDataSettings.Value.WebmasterEmailAddress));

			if (message.To.Count == 0 && _contactDataSettings.Value.CompanyEmailAddress.HasValue())
				message.To.Add(new EmailAddress(_contactDataSettings.Value.CompanyEmailAddress));

			if (message.To.Count == 0)
				message.To.Add(new EmailAddress(emailAccount.Email, emailAccount.DisplayName));

			message.Subject = _services.Localization.GetResource("Admin.DataExchange.Export.CompletedEmail.Subject", languageId)
				.FormatInvariant(ctx.Request.Profile.Name);

			message.Body = body.ToString();

#if DEBUG
			//_queuedEmailService.Value.InsertQueuedEmail(new QueuedEmail
			//{
			//	From = emailAccount.Email,
			//	FromName = emailAccount.DisplayName,
			//	To = message.To.First().Address,
			//	Subject = message.Subject,
			//	Body = message.Body,
			//	CreatedOnUtc = DateTime.UtcNow,
			//	EmailAccountId = emailAccount.Id,
			//	SendManually = true
			//});
			//_dbContext.SaveChanges();
#else
        _emailSender.Value.SendEmail(smtpContext, message);
#endif
		}

		#endregion

		#region Getting data

		private IQueryable<Product> GetProductQuery(DataExporterContext ctx, int skip, int take)
		{
			IQueryable<Product> query = null;

			if (ctx.Request.ProductQuery == null)
			{
				var searchContext = new ProductSearchContext
				{
					OrderBy = ProductSortingEnum.CreatedOn,
					ProductIds = ctx.Request.EntitiesToExport,
					StoreId = (ctx.Request.Profile.PerStore ? ctx.Store.Id : ctx.Filter.StoreId),
					VisibleIndividuallyOnly = true,
					PriceMin = ctx.Filter.PriceMinimum,
					PriceMax = ctx.Filter.PriceMaximum,
					IsPublished = ctx.Filter.IsPublished,
					WithoutCategories = ctx.Filter.WithoutCategories,
					WithoutManufacturers = ctx.Filter.WithoutManufacturers,
					ManufacturerId = ctx.Filter.ManufacturerId ?? 0,
					FeaturedProducts = ctx.Filter.FeaturedProducts,
					ProductType = ctx.Filter.ProductType,
					ProductTagId = ctx.Filter.ProductTagId ?? 0,
					IdMin = ctx.Filter.IdMinimum ?? 0,
					IdMax = ctx.Filter.IdMaximum ?? 0,
					AvailabilityMinimum = ctx.Filter.AvailabilityMinimum,
					AvailabilityMaximum = ctx.Filter.AvailabilityMaximum
				};

				if (!ctx.Filter.IsPublished.HasValue)
					searchContext.ShowHidden = true;

				if (ctx.Filter.CategoryIds != null && ctx.Filter.CategoryIds.Length > 0)
					searchContext.CategoryIds = ctx.Filter.CategoryIds.ToList();

				if (ctx.Filter.CreatedFrom.HasValue)
					searchContext.CreatedFromUtc = _dateTimeHelper.Value.ConvertToUtcTime(ctx.Filter.CreatedFrom.Value, _dateTimeHelper.Value.CurrentTimeZone);

				if (ctx.Filter.CreatedTo.HasValue)
					searchContext.CreatedToUtc = _dateTimeHelper.Value.ConvertToUtcTime(ctx.Filter.CreatedTo.Value, _dateTimeHelper.Value.CurrentTimeZone);

				query = _productService.Value.PrepareProductSearchQuery(searchContext);

				query = query.OrderByDescending(x => x.CreatedOnUtc);
			}
			else
			{
				query = ctx.Request.ProductQuery;
			}

			if (skip > 0)
				query = query.Skip(skip);

			if (take != int.MaxValue)
				query = query.Take(take);

			return query;
		}

		private List<Product> GetProducts(DataExporterContext ctx, int skip)
		{
			// we use ctx.EntityIdsPerSegment to avoid exporting products multiple times per segment\file (cause of associated products).

			var result = new List<Product>();

			var products = GetProductQuery(ctx, skip, PageSize).ToList();

			foreach (var product in products)
			{
				if (product.ProductType == ProductType.SimpleProduct || product.ProductType == ProductType.BundledProduct)
				{
					if (!ctx.EntityIdsPerSegment.Contains(product.Id))
					{
						result.Add(product);
						ctx.EntityIdsPerSegment.Add(product.Id);
					}
				}
				else if (product.ProductType == ProductType.GroupedProduct)
				{
					if (ctx.Projection.NoGroupedProducts && !ctx.IsPreview)
					{
						var associatedSearchContext = new ProductSearchContext
						{
							OrderBy = ProductSortingEnum.CreatedOn,
							PageSize = int.MaxValue,
							StoreId = (ctx.Request.Profile.PerStore ? ctx.Store.Id : ctx.Filter.StoreId),
							VisibleIndividuallyOnly = false,
							ParentGroupedProductId = product.Id
						};

						foreach (var associatedProduct in _productService.Value.SearchProducts(associatedSearchContext))
						{
							if (!ctx.EntityIdsPerSegment.Contains(associatedProduct.Id))
							{
								result.Add(associatedProduct);
								ctx.EntityIdsPerSegment.Add(associatedProduct.Id);
							}
						}
					}
					else
					{
						if (!ctx.EntityIdsPerSegment.Contains(product.Id))
						{
							result.Add(product);
							ctx.EntityIdsPerSegment.Add(product.Id);
						}
					}
				}
			}

			SetProgress(ctx, products.Count);

			return result;
		}

		private IQueryable<Order> GetOrderQuery(DataExporterContext ctx, int skip, int take)
		{
			var query = _orderService.Value.GetOrders(
				ctx.Request.Profile.PerStore ? ctx.Store.Id : ctx.Filter.StoreId,
				ctx.Projection.CustomerId ?? 0,
				ctx.Filter.CreatedFrom.HasValue ? (DateTime?)_dateTimeHelper.Value.ConvertToUtcTime(ctx.Filter.CreatedFrom.Value, _dateTimeHelper.Value.CurrentTimeZone) : null,
				ctx.Filter.CreatedTo.HasValue ? (DateTime?)_dateTimeHelper.Value.ConvertToUtcTime(ctx.Filter.CreatedTo.Value, _dateTimeHelper.Value.CurrentTimeZone) : null,
				ctx.Filter.OrderStatusIds,
				ctx.Filter.PaymentStatusIds,
				ctx.Filter.ShippingStatusIds,
				null,
				null,
				null);

			if (ctx.Request.EntitiesToExport.Count > 0)
				query = query.Where(x => ctx.Request.EntitiesToExport.Contains(x.Id));

			query = query.OrderByDescending(x => x.CreatedOnUtc);

			if (skip > 0)
				query = query.Skip(skip);

			if (take != int.MaxValue)
				query = query.Take(take);

			return query;
		}

		private List<Order> GetOrders(DataExporterContext ctx, int skip)
		{
			var orders = GetOrderQuery(ctx, skip, PageSize).ToList();

			if (ctx.Projection.OrderStatusChange != ExportOrderStatusChange.None)
			{
				ctx.SetLoadedEntityIds(orders.Select(x => x.Id));
			}

			SetProgress(ctx, orders.Count);

			return orders;
		}

		private IQueryable<Manufacturer> GetManufacturerQuery(DataExporterContext ctx, int skip, int take)
		{
			var showHidden = !ctx.Filter.IsPublished.HasValue;
			var storeId = (ctx.Request.Profile.PerStore ? ctx.Store.Id : ctx.Filter.StoreId);

			var query = _manufacturerService.Value.GetManufacturers(showHidden, storeId);

			if (ctx.Request.EntitiesToExport.Count > 0)
				query = query.Where(x => ctx.Request.EntitiesToExport.Contains(x.Id));

			query = query.OrderBy(x => x.DisplayOrder);

			if (skip > 0)
				query = query.Skip(skip);

			if (take != int.MaxValue)
				query = query.Take(take);

			return query;
		}

		private List<Manufacturer> GetManufacturers(DataExporterContext ctx, int skip)
		{
			var manus = GetManufacturerQuery(ctx, skip, PageSize).ToList();

			SetProgress(ctx, manus.Count);

			return manus;
		}

		private IQueryable<Category> GetCategoryQuery(DataExporterContext ctx, int skip, int take)
		{
			var showHidden = !ctx.Filter.IsPublished.HasValue;
			var storeId = (ctx.Request.Profile.PerStore ? ctx.Store.Id : ctx.Filter.StoreId);

			var query = _categoryService.Value.GetCategories(null, showHidden, null, true, storeId);

			if (ctx.Request.EntitiesToExport.Count > 0)
				query = query.Where(x => ctx.Request.EntitiesToExport.Contains(x.Id));

			query = query
				.OrderBy(x => x.ParentCategoryId)
				.ThenBy(x => x.DisplayOrder);

			if (skip > 0)
				query = query.Skip(skip);

			if (take != int.MaxValue)
				query = query.Take(take);

			return query;
		}

		private List<Category> GetCategories(DataExporterContext ctx, int skip)
		{
			var categories = GetCategoryQuery(ctx, skip, PageSize).ToList();

			SetProgress(ctx, categories.Count);

			return categories;
		}

		private IQueryable<Customer> GetCustomerQuery(DataExporterContext ctx, int skip, int take)
		{
			var query = _customerRepository.Value.TableUntracked
				.Expand(x => x.BillingAddress)
				.Expand(x => x.ShippingAddress)
				.Expand(x => x.Addresses.Select(y => y.Country))
				.Expand(x => x.Addresses.Select(y => y.StateProvince))
				.Expand(x => x.CustomerRoles)
				.Where(x => !x.Deleted);

			if (ctx.Request.EntitiesToExport.Count > 0)
				query = query.Where(x => ctx.Request.EntitiesToExport.Contains(x.Id));

			query = query.OrderByDescending(x => x.CreatedOnUtc);

			if (skip > 0)
				query = query.Skip(skip);

			if (take != int.MaxValue)
				query = query.Take(take);

			return query;
		}

		private List<Customer> GetCustomers(DataExporterContext ctx, int skip)
		{
			var customers = GetCustomerQuery(ctx, skip, PageSize).ToList();

			SetProgress(ctx, customers.Count);

			return customers;
		}

		private IQueryable<NewsLetterSubscription> GetNewsLetterSubscriptionQuery(DataExporterContext ctx, int skip, int take)
		{
			var storeId = (ctx.Request.Profile.PerStore ? ctx.Store.Id : ctx.Filter.StoreId);

			var query = _subscriptionRepository.Value.TableUntracked;

			if (storeId > 0)
				query = query.Where(x => x.StoreId == storeId);

			if (ctx.Request.EntitiesToExport.Count > 0)
				query = query.Where(x => ctx.Request.EntitiesToExport.Contains(x.Id));

			query = query
				.OrderBy(x => x.StoreId)
				.ThenBy(x => x.Email);

			if (skip > 0)
				query = query.Skip(skip);

			if (take != int.MaxValue)
				query = query.Take(take);

			return query;
		}

		private List<NewsLetterSubscription> GetNewsLetterSubscriptions(DataExporterContext ctx, int skip)
		{
			var subscriptions = GetNewsLetterSubscriptionQuery(ctx, skip, PageSize).ToList();

			SetProgress(ctx, subscriptions.Count);

			return subscriptions;
		}

		#endregion

		private List<Store> Init(DataExporterContext ctx, int? totalRecords = null)
		{
			// Init base things that are even required for preview. Init all other things (regular export) in ExportCoreOuter.
			List<Store> result = null;

			if (ctx.Request.CustomerId == 0)
				ctx.Request.CustomerId = _services.WorkContext.CurrentCustomer.Id;

			if (ctx.Projection.CurrencyId.HasValue)
				ctx.ContextCurrency = _currencyService.Value.GetCurrencyById(ctx.Projection.CurrencyId.Value);
			else
				ctx.ContextCurrency = _services.WorkContext.WorkingCurrency;

			if (ctx.Projection.CustomerId.HasValue)
				ctx.ContextCustomer = _customerService.GetCustomerById(ctx.Projection.CustomerId.Value);
			else
				ctx.ContextCustomer = _services.WorkContext.CurrentCustomer;

			if (ctx.Projection.LanguageId.HasValue)
				ctx.ContextLanguage = _languageService.Value.GetLanguageById(ctx.Projection.LanguageId.Value);
			else
				ctx.ContextLanguage = _services.WorkContext.WorkingLanguage;

			ctx.Stores = _services.StoreService.GetAllStores().ToDictionary(x => x.Id, x => x);
			ctx.Languages = _languageService.Value.GetAllLanguages(true).ToDictionary(x => x.Id, x => x);

			if (!ctx.IsPreview && ctx.Request.Profile.PerStore)
			{
				result = new List<Store>(ctx.Stores.Values.Where(x => x.Id == ctx.Filter.StoreId || ctx.Filter.StoreId == 0));
			}
			else
			{
				int? storeId = (ctx.Filter.StoreId == 0 ? ctx.Projection.StoreId : ctx.Filter.StoreId);

				ctx.Store = ctx.Stores.Values.FirstOrDefault(x => x.Id == (storeId ?? _services.StoreContext.CurrentStore.Id));

				result = new List<Store> { ctx.Store };
			}

			// get total records for progress
			foreach (var store in result)
			{
				ctx.Store = store;

				int totalCount = 0;

				if (totalRecords.HasValue)
				{
					totalCount = totalRecords.Value;    // speed up preview by not counting total at each page
				}
				else
				{
					switch (ctx.Request.Provider.Value.EntityType)
					{
						case ExportEntityType.Product:
							totalCount = GetProductQuery(ctx, ctx.Request.Profile.Offset, int.MaxValue).Count();
							break;
						case ExportEntityType.Order:
							totalCount = GetOrderQuery(ctx, ctx.Request.Profile.Offset, int.MaxValue).Count();
							break;
						case ExportEntityType.Manufacturer:
							totalCount = GetManufacturerQuery(ctx, ctx.Request.Profile.Offset, int.MaxValue).Count();
							break;
						case ExportEntityType.Category:
							totalCount = GetCategoryQuery(ctx, ctx.Request.Profile.Offset, int.MaxValue).Count();
							break;
						case ExportEntityType.Customer:
							totalCount = GetCustomerQuery(ctx, ctx.Request.Profile.Offset, int.MaxValue).Count();
							break;
						case ExportEntityType.NewsLetterSubscription:
							totalCount = GetNewsLetterSubscriptionQuery(ctx, ctx.Request.Profile.Offset, int.MaxValue).Count();
							break;
					}
				}

				ctx.RecordsPerStore.Add(store.Id, totalCount);
			}

			return result;
		}

		private void ExportCoreInner(DataExporterContext ctx, Store store)
		{
			if (ctx.ExecuteContext.Abort != DataExchangeAbortion.None)
				return;

			int fileIndex = 0;

			ctx.Store = store;

			{
				var logHead = new StringBuilder();
				logHead.AppendLine();
				logHead.AppendLine(new string('-', 40));
				logHead.AppendLine("SmartStore.NET:\t\tv." + SmartStoreVersion.CurrentFullVersion);
				logHead.Append("Export profile:\t\t" + ctx.Request.Profile.Name);
				logHead.AppendLine(ctx.Request.Profile.Id == 0 ? " (volatile)" : " (Id {0})".FormatInvariant(ctx.Request.Profile.Id));

				logHead.AppendLine("Export provider:\t{0} ({1})".FormatInvariant(ctx.Request.Provider.Metadata.FriendlyName, ctx.Request.Profile.ProviderSystemName));

				var plugin = ctx.Request.Provider.Metadata.PluginDescriptor;
				logHead.Append("Plugin:\t\t\t\t");
				logHead.AppendLine(plugin == null ? "".NaIfEmpty() : "{0} ({1}) v.{2}".FormatInvariant(plugin.FriendlyName, plugin.SystemName, plugin.Version.ToString()));

				logHead.AppendLine("Entity:\t\t\t\t" + ctx.Request.Provider.Value.EntityType.ToString());

				var storeInfo = (ctx.Request.Profile.PerStore ? "{0} (Id {1})".FormatInvariant(ctx.Store.Name, ctx.Store.Id) : "All stores");
				logHead.Append("Store:\t\t\t\t" + storeInfo);

				ctx.Log.Information(logHead.ToString());
			}

			ctx.ExecuteContext.Store = ToDynamic(ctx, ctx.Store);

			ctx.ExecuteContext.MaxFileNameLength = _dataExchangeSettings.Value.MaxFileNameLength;

			ctx.ExecuteContext.HasPublicDeployment = ctx.Request.Profile.Deployments.Any(x => x.IsPublic && x.DeploymentType == ExportDeploymentType.FileSystem);

			ctx.ExecuteContext.PublicFolderPath = (ctx.ExecuteContext.HasPublicDeployment ? Path.Combine(HttpRuntime.AppDomainAppPath, PublicFolder) : null);

			var fileExtension = (ctx.Request.Provider.Value.FileExtension.HasValue() ? ctx.Request.Provider.Value.FileExtension.ToLower().EnsureStartsWith(".") : "");


			using (var segmenter = CreateSegmenter(ctx))
			{
				if (segmenter == null)
				{
					throw new SmartException("Unsupported entity type '{0}'.".FormatInvariant(ctx.Request.Provider.Value.EntityType.ToString()));
				}

				if (segmenter.TotalRecords <= 0)
				{
					ctx.Log.Information("There are no records to export.");
				}

				while (ctx.ExecuteContext.Abort == DataExchangeAbortion.None && segmenter.HasData)
				{
					segmenter.RecordPerSegmentCount = 0;
					ctx.ExecuteContext.RecordsSucceeded = 0;

					string path = null;

					if (ctx.IsFileBasedExport)
					{
						var resolvedPattern = ctx.Request.Profile.ResolveFileNamePattern(ctx.Store, ++fileIndex, ctx.ExecuteContext.MaxFileNameLength);

						ctx.ExecuteContext.FileName = resolvedPattern + fileExtension;
						path = Path.Combine(ctx.ExecuteContext.Folder, ctx.ExecuteContext.FileName);

						if (ctx.ExecuteContext.HasPublicDeployment)
							ctx.ExecuteContext.PublicFileUrl = ctx.Store.Url.EnsureEndsWith("/") + PublicFolder.EnsureEndsWith("/") + ctx.ExecuteContext.FileName;
					}

					if (CallProvider(ctx, null, "Execute", path))
					{
						ctx.Log.Information("Provider reports {0} successfully exported record(s).".FormatInvariant(ctx.ExecuteContext.RecordsSucceeded));

						// create info for deployment list in profile edit
						if (ctx.IsFileBasedExport)
						{
							ctx.Result.Files.Add(new DataExportResult.ExportFileInfo
							{
								StoreId = ctx.Store.Id,
								FileName = ctx.ExecuteContext.FileName
							});
						}
					}

					ctx.EntityIdsPerSegment.Clear();

					if (ctx.ExecuteContext.IsMaxFailures)
						ctx.Log.Warning("Export aborted. The maximum number of failures has been reached.");

					if (ctx.CancellationToken.IsCancellationRequested)
						ctx.Log.Warning("Export aborted. A cancellation has been requested.");

					DetachAllEntitiesAndClear(ctx);
				}

				if (ctx.ExecuteContext.Abort != DataExchangeAbortion.Hard)
				{
					// always call OnExecuted
					if (ctx.ExecuteContext.ExtraDataStreams.Count == 0)
						ctx.ExecuteContext.ExtraDataStreams.Add(new ExportExtraStreams());

					ctx.ExecuteContext.ExtraDataStreams.ForEach(x =>
					{
						var path = (x.FileName.HasValue() ? Path.Combine(ctx.ExecuteContext.Folder, x.FileName) : null);

						CallProvider(ctx, x.Id, "OnExecuted", path);
					});

					ctx.ExecuteContext.ExtraDataStreams.Clear();
				}
			}
		}

		private void ExportCoreOuter(DataExporterContext ctx)
		{
			if (ctx.Request.Profile == null || !ctx.Request.Profile.Enabled)
				return;

			var logPath = ctx.Request.Profile.GetExportLogPath();
			var zipPath = ctx.Request.Profile.GetExportZipPath();

            FileSystemHelper.Delete(logPath);
			FileSystemHelper.Delete(zipPath);
			FileSystemHelper.ClearDirectory(ctx.FolderContent, false);

			using (var logger = new TraceLogger(logPath))
			{
				try
				{
					ctx.Log = logger;
					ctx.ExecuteContext.Log = logger;
					ctx.ProgressInfo = T("Admin.DataExchange.Export.ProgressInfo");

					if (!ctx.Request.Provider.IsValid())
						throw new SmartException("Export aborted because the export provider is not valid.");

					if (!HasPermission(ctx))
						throw new SmartException("You do not have permission to perform the selected export.");

					foreach (var item in ctx.Request.CustomData)
					{
						ctx.ExecuteContext.CustomProperties.Add(item.Key, item.Value);
					}

					if (ctx.Request.Profile.ProviderConfigData.HasValue())
					{
						var configInfo = ctx.Request.Provider.Value.ConfigurationInfo;
						if (configInfo != null)
						{
							ctx.ExecuteContext.ConfigurationData = XmlHelper.Deserialize(ctx.Request.Profile.ProviderConfigData, configInfo.ModelType);
						}
					}

					// lazyLoading: false, proxyCreation: false impossible. how to identify all properties of all data levels of all entities
					// that require manual resolving for now and for future? fragile, susceptible to faults (e.g. price calculation)...
					using (var scope = new DbContextScope(_dbContext, autoDetectChanges: false, proxyCreation: true, validateOnSave: false, forceNoTracking: true))
					{
						ctx.DeliveryTimes = _deliveryTimeService.Value.GetAllDeliveryTimes().ToDictionary(x => x.Id);
						ctx.QuantityUnits = _quantityUnitService.Value.GetAllQuantityUnits().ToDictionary(x => x.Id);
						ctx.ProductTemplates = _productTemplateService.Value.GetAllProductTemplates().ToDictionary(x => x.Id);

						if (ctx.Request.Provider.Value.EntityType == ExportEntityType.Product)
						{
							var allCategories = _categoryService.Value.GetAllCategories(showHidden: true, applyNavigationFilters: false);
							ctx.Categories = allCategories.ToDictionary(x => x.Id);
						}

						if (ctx.Request.Provider.Value.EntityType == ExportEntityType.Order)
						{
							ctx.Countries = _countryService.Value.GetAllCountries(true).ToDictionary(x => x.Id, x => x);
						}

						if (ctx.Request.Provider.Value.EntityType == ExportEntityType.Customer)
						{
							var subscriptionEmails = _subscriptionRepository.Value.TableUntracked
								.Where(x => x.Active)
								.Select(x => x.Email)
								.Distinct()
								.ToList();

							ctx.NewsletterSubscriptions = new HashSet<string>(subscriptionEmails, StringComparer.OrdinalIgnoreCase);
						}

						var stores = Init(ctx);

						ctx.ExecuteContext.Language = ToDynamic(ctx, ctx.ContextLanguage);
						ctx.ExecuteContext.Customer = ToDynamic(ctx, ctx.ContextCustomer);
						ctx.ExecuteContext.Currency = ToDynamic(ctx, ctx.ContextCurrency);

						stores.ForEach(x => ExportCoreInner(ctx, x));
					}

					if (!ctx.IsPreview && ctx.ExecuteContext.Abort != DataExchangeAbortion.Hard)
					{
						if (ctx.IsFileBasedExport)
						{
							if (ctx.Request.Profile.CreateZipArchive || ctx.Request.Profile.Deployments.Any(x => x.Enabled && x.CreateZip))
							{
								ZipFile.CreateFromDirectory(ctx.FolderContent, zipPath, CompressionLevel.Fastest, true);
							}

							if (ctx.Request.Profile.Deployments.Any(x => x.Enabled))
							{
								SetProgress(ctx, T("Common.Deployment"));

								Deploy(ctx, zipPath);
							}
						}

						if (ctx.Request.Profile.EmailAccountId != 0 && ctx.Request.Profile.CompletedEmailAddresses.HasValue())
						{
							SendCompletionEmail(ctx, zipPath);
						}
						else if (ctx.Request.Profile.IsSystemProfile && !ctx.Supports(ExportFeatures.CanOmitCompletionMail))
						{
							SendCompletionEmail(ctx, zipPath);
						}
					}
				}
				catch (Exception exception)
				{
					logger.ErrorsAll(exception);
					ctx.Result.LastError = exception.ToString();
				}
				finally
				{
					try
					{
						if (!ctx.IsPreview && ctx.Request.Profile.Id != 0)
						{
							ctx.Request.Profile.ResultInfo = XmlHelper.Serialize<DataExportResult>(ctx.Result);

							_exportProfileService.Value.UpdateExportProfile(ctx.Request.Profile);
						}
					}
					catch (Exception exception)
					{
						logger.ErrorsAll(exception);
					}

					try
					{
						if (ctx.IsFileBasedExport && ctx.ExecuteContext.Abort != DataExchangeAbortion.Hard && ctx.Request.Profile.Cleanup)
						{
							FileSystemHelper.ClearDirectory(ctx.FolderContent, false);
						}
					}
					catch (Exception exception)
					{
						logger.ErrorsAll(exception);
					}

					DetachAllEntitiesAndClear(ctx);

					try
					{
						ctx.NewsletterSubscriptions.Clear();
						ctx.ProductTemplates.Clear();
						ctx.Countries.Clear();
						ctx.Languages.Clear();
						ctx.QuantityUnits.Clear();
						ctx.DeliveryTimes.Clear();
						ctx.CategoryPathes.Clear();
						ctx.Categories.Clear();
						ctx.Stores.Clear();

						ctx.Request.CustomData.Clear();

						ctx.ExecuteContext.CustomProperties.Clear();
						ctx.ExecuteContext.Log = null;
						ctx.Log = null;
					}
					catch (Exception exception)
					{
						logger.ErrorsAll(exception);
					}
				}
			}

			if (ctx.IsPreview || ctx.ExecuteContext.Abort == DataExchangeAbortion.Hard)
				return;

			// post process order entities
			if (ctx.EntityIdsLoaded.Count > 0 && ctx.Request.Provider.Value.EntityType == ExportEntityType.Order && ctx.Projection.OrderStatusChange != ExportOrderStatusChange.None)
			{
				using (var logger = new TraceLogger(logPath))
				{
					try
					{
						int? orderStatusId = null;

						if (ctx.Projection.OrderStatusChange == ExportOrderStatusChange.Processing)
							orderStatusId = (int)OrderStatus.Processing;
						else if (ctx.Projection.OrderStatusChange == ExportOrderStatusChange.Complete)
							orderStatusId = (int)OrderStatus.Complete;

						using (var scope = new DbContextScope(_dbContext, false, null, false, false, false, false))
						{
							foreach (var chunk in ctx.EntityIdsLoaded.Chunk())
							{
								var entities = _orderRepository.Value.Table.Where(x => chunk.Contains(x.Id)).ToList();

								entities.ForEach(x => x.OrderStatusId = (orderStatusId ?? x.OrderStatusId));

								_dbContext.SaveChanges();
							}
						}

						logger.Information("Updated order status for {0} order(s).".FormatInvariant(ctx.EntityIdsLoaded.Count()));
					}
					catch (Exception exception)
					{
						logger.ErrorsAll(exception);
						ctx.Result.LastError = exception.ToString();
					}
				}
			}
		}

		/// <summary>
		/// The name of the public export folder
		/// </summary>
		public static string PublicFolder
		{
			get { return "Exchange"; }
		}

		public static int PageSize
		{
			get { return 100; }
		}

		public DataExportResult Export(DataExportRequest request, CancellationToken cancellationToken)
		{
			var ctx = new DataExporterContext(request, cancellationToken);

			ExportCoreOuter(ctx);

			if (ctx.Result != null && ctx.Result.Succeeded && ctx.Result.Files.Count > 0)
			{
				string prefix = null;
				string suffix = null;
				var extension = Path.GetExtension(ctx.Result.Files.First().FileName);
				var provider = request.Provider.Value;

				if (provider.EntityType == ExportEntityType.Product)
					prefix = T("Admin.Catalog.Products");
				else if (provider.EntityType == ExportEntityType.Order)
					prefix = T("Admin.Orders");
				else if (provider.EntityType == ExportEntityType.Category)
					prefix = T("Admin.Catalog.Categories");
				else if (provider.EntityType == ExportEntityType.Manufacturer)
					prefix = T("Admin.Catalog.Manufacturers");
				else if (provider.EntityType == ExportEntityType.Customer)
					prefix = T("Admin.Customers");
				else if (provider.EntityType == ExportEntityType.NewsLetterSubscription)
					prefix = T("Admin.Promotions.NewsLetterSubscriptions");
				else
					prefix = provider.EntityType.ToString();

				var selectedEntityCount = (request.EntitiesToExport == null ? 0 : request.EntitiesToExport.Count);

				if (selectedEntityCount == 0)
					suffix = T("Common.All");
				else
					suffix = (selectedEntityCount == 1 ? request.EntitiesToExport.First().ToString() : T("Admin.Common.Selected").Text);

				ctx.Result.DownloadFileName = string.Concat(prefix, "-", suffix).ToLower().ToValidFileName() + extension;
			}

			cancellationToken.ThrowIfCancellationRequested();

			return ctx.Result;
		}

		public IList<dynamic> Preview(DataExportRequest request, int pageIndex, int? totalRecords = null)
		{
			var resultData = new List<dynamic>();
			var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(5.0));

			var ctx = new DataExporterContext(request, cancellation.Token, true);

			var unused = Init(ctx, totalRecords);

			if (!HasPermission(ctx))
				throw new SmartException("You do not have permission to perform the selected export");

			using (var segmenter = CreateSegmenter(ctx, pageIndex))
			{
				if (segmenter == null)
				{
					throw new SmartException("Unsupported entity type '{0}'".FormatInvariant(ctx.Request.Provider.Value.EntityType.ToString()));
				}

				while (segmenter.HasData)
				{
					segmenter.RecordPerSegmentCount = 0;

					while (segmenter.ReadNextSegment())
					{
						resultData.AddRange(segmenter.CurrentSegment);
					}
				}

				DetachAllEntitiesAndClear(ctx);
			}

			if (ctx.Result.LastError.HasValue())
			{
				_services.Notifier.Error(ctx.Result.LastError);
			}

			return resultData;
		}

		public int GetDataCount(DataExportRequest request)
		{
			var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(5.0));

			var ctx = new DataExporterContext(request, cancellation.Token, true);

			var unused = Init(ctx);

			var totalCount = ctx.RecordsPerStore.First().Value;

			return totalCount;
		}
	}
}
