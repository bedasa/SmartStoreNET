﻿using System;
using System.IO;
using System.Linq;
using SmartStore.Core.Data;
using SmartStore.Core.Domain;
using SmartStore.Core.Domain.DataExchange;
using SmartStore.Core.Domain.Tasks;
using SmartStore.Core.Events;
using SmartStore.Services.Tasks;
using SmartStore.Utilities;

namespace SmartStore.Services.DataExchange.Import
{
	public partial class ImportProfileService : IImportProfileService
	{
		private readonly IRepository<ImportProfile> _importProfileRepository;
		private readonly IEventPublisher _eventPublisher;
		private readonly IScheduleTaskService _scheduleTaskService;
		private readonly DataExchangeSettings _dataExchangeSettings;

		public ImportProfileService(
			IRepository<ImportProfile> importProfileRepository,
			IEventPublisher eventPublisher,
			IScheduleTaskService scheduleTaskService,
			DataExchangeSettings dataExchangeSettings)
		{
			_importProfileRepository = importProfileRepository;
			_eventPublisher = eventPublisher;
			_scheduleTaskService = scheduleTaskService;
			_dataExchangeSettings = dataExchangeSettings;
		}

		public virtual ImportProfile InsertImportProfile(string fileName, string name, ImportEntityType entityType)
		{
			Guard.ArgumentNotEmpty(() => fileName);
			Guard.ArgumentNotEmpty(() => name);

			var task = new ScheduleTask
			{
				CronExpression = "0 */24 * * *",
				Type = typeof(DataImportTask).AssemblyQualifiedNameWithoutVersion(),
				Enabled = false,
				StopOnError = false,
				IsHidden = true
			};

			task.Name = string.Concat(name, " Task");

			_scheduleTaskService.InsertTask(task);

			var profile = new ImportProfile
			{
				Name = name,
				EntityType = entityType,
				Enabled = true,
				SchedulingTaskId = task.Id
			};

			if (Path.GetExtension(fileName).IsCaseInsensitiveEqual(".xlsx"))
				profile.FileType = ImportFileType.XLSX;
			else
				profile.FileType = ImportFileType.CSV;

			profile.FolderName = SeoHelper.GetSeName(name, true, false)
				.ToValidPath()
				.Truncate(_dataExchangeSettings.MaxFileNameLength);

			profile.FolderName = FileSystemHelper.CreateNonExistingDirectoryName(CommonHelper.MapPath("~/App_Data/ImportProfiles"), profile.FolderName);

			_importProfileRepository.Insert(profile);

			task.Alias = profile.Id.ToString();
			_scheduleTaskService.UpdateTask(task);

			_eventPublisher.EntityInserted(profile);

			return profile;
		}

		public virtual void UpdateImportProfile(ImportProfile profile)
		{
			if (profile == null)
				throw new ArgumentNullException("profile");

			_importProfileRepository.Update(profile);

			_eventPublisher.EntityUpdated(profile);
		}

		public virtual void DeleteImportProfile(ImportProfile profile)
		{
			if (profile == null)
				throw new ArgumentNullException("profile");

			var scheduleTaskId = profile.SchedulingTaskId;
			var folder = profile.GetImportFolder();

			_importProfileRepository.Delete(profile);

			var scheduleTask = _scheduleTaskService.GetTaskById(scheduleTaskId);
			_scheduleTaskService.DeleteTask(scheduleTask);

			_eventPublisher.EntityDeleted(profile);

			if (System.IO.Directory.Exists(folder))
			{
				FileSystemHelper.ClearDirectory(folder, true);
			}
		}

		public virtual IQueryable<ImportProfile> GetImportProfiles(bool? enabled = null)
		{
			var query = _importProfileRepository.Table
				.Expand(x => x.ScheduleTask);

			if (enabled.HasValue)
			{
				query = query.Where(x => x.Enabled == enabled.Value);
			}

			query = query
				.OrderBy(x => x.EntityTypeId)
				.ThenBy(x => x.Name);

			return query;
		}

		public virtual ImportProfile GetImportProfileById(int id)
		{
			if (id == 0)
				return null;

			var profile = _importProfileRepository.Table
				.Expand(x => x.ScheduleTask)
				.FirstOrDefault(x => x.Id == id);

			return profile;
		}
	}
}