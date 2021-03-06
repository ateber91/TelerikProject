﻿using Microsoft.EntityFrameworkCore;
using SD.Data.Context;
using SD.Data.Models.DomainModels;
using SD.Services.Data.Exceptions;
using SD.Services.Data.Services.Contracts;
using SD.Services.Data.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using X.PagedList;

namespace SD.Services.Data.Services
{
	public class UserSensorService : IUserSensorService
    {
        private readonly DataContext dataContext;
		private readonly ISensorService sensorService;

        public UserSensorService(DataContext dataContext, ISensorService sensorService)
        {
            this.dataContext = dataContext ?? throw new ArgumentNullException(nameof(dataContext));
            this.sensorService = sensorService ?? throw new ArgumentNullException(nameof(sensorService));
		}

        public async Task<IPagedList<UserSensor>> FilterUserSensorsAsync(string filter = "", int pageNumber = 1, int pageSize = 10)
        {
            Validator.ValidateNull(filter, "Filter cannot be null!");

            Validator.ValidateMinRange(pageNumber, 1, "Page number cannot be less then 1!");
            Validator.ValidateMinRange(pageSize, 0, "Page size cannot be less then 0!");

            var query = this.dataContext.UserSensors
				.Include(us => us.Sensor)
				.Where(t => t.Name.Contains(filter));


            return await query.ToPagedListAsync(pageNumber, pageSize);
        }

        public async Task<IPagedList<UserSensor>> GetSensorsByUserId(string userId, int pageNumber = 1, int pageSize = 10)
        {
            Validator.ValidateMinRange(pageNumber, 1, "Page number cannot be less then 1!");
            Validator.ValidateMinRange(pageSize, 0, "Page size cannot be less then 0!");

            var query = this.dataContext.UserSensors
				.Include(us => us.Sensor)
                .Where(us => us.UserId.Equals(userId));

            return await query.ToPagedListAsync(pageNumber, pageSize);
        }

        public async Task<UserSensor> GetSensorByIdAsync(string id)
        {
			var userSensor = await this.dataContext.UserSensors
				.Include(us => us.Sensor)
                .Include(x => x.User)
                .FirstOrDefaultAsync(us => us.Id == id);

            Validator.ValidateNull(userSensor, "userSensor is null");

            return userSensor;
        }

        public async Task<UserSensor> AddUserSensorAsync(string userId, string sensorId, string name, string description,
            string latitude, string longitude, double alarmMin, double alarmMax, int pollingInterval, bool alarmTriggered, 
			bool isPublic, string lastValue, string type)
        {
            Validator.ValidateNull(name, "Sensor name cannot be null!");

            if (await this.dataContext.UserSensors.AnyAsync(us => us.Name.Equals(name)))
            {
                throw new EntityAlreadyExistsException("User sensor already exists!");
            }

            var sensor = await this.dataContext.Sensors.FirstOrDefaultAsync(ss => ss.Id == sensorId);

            var userSensor = new UserSensor
            {
                UserId = userId,
                Name = name,
                Description = description,
                Latitude = latitude,
                Longitude = longitude,
                AlarmTriggered = alarmTriggered,
                AlarmMin = alarmMin,
                AlarmMax = alarmMax,
                IsPublic = isPublic,
                PollingInterval = pollingInterval,
                Coordinates = latitude + "," + longitude,
                Type = type,
                LastValueUser = lastValue,
                SensorId = sensor.Id,
                UserInterval = pollingInterval
            };
            

            await this.dataContext.UserSensors.AddAsync(userSensor);
            await this.dataContext.SaveChangesAsync();
            return userSensor;
        }

        public async Task UpdateUserSensorAsync(UserSensor userSensor)
        {
            Validator.ValidateNull(userSensor, "userSensor is null");

            this.dataContext.Update(userSensor);
            await this.dataContext.SaveChangesAsync();
        }

        public async Task<IEnumerable<UserSensor>> ListSensorsForUserAsync(string userId)
        {
            return await this.dataContext.UserSensors
				.Include(us => us.Sensor)
				.Where(se => se.IsDeleted == false && se.UserId == userId)
				.ToListAsync();
        }

        public async Task<IEnumerable<UserSensor>> ListPublicSensorsAsync()
        {
            return await this.dataContext.UserSensors
				.Include(us => us.Sensor)
				.Where(se => se.IsDeleted == false && se.IsPublic == true).ToListAsync();
        }

        public async Task<UserSensor> ListSensorByIdAsync(string sensorId)
        {
            return await this.dataContext.UserSensors
				.Include(us => us.Sensor)
				.FirstOrDefaultAsync(se => se.Id == sensorId);
        }

		public async Task<UserSensor> DisableUserSensor(string userSensorId)
		{
			Validator.ValidateNull(userSensorId, "User sensor Id cannot be null!");
			Validator.ValidateGuid(userSensorId, "User sensor id is not in the correct format.Unable to parse to Guid!");

			UserSensor userSensor = await this.dataContext.UserSensors
				.Include(us => us.Sensor)
				.FirstAsync(us => us.Id.Equals(userSensorId));

			if (userSensorId == null)
			{
				throw new EntityNotFoundException();
			}

			this.dataContext.Remove(userSensor);
			await this.dataContext.SaveChangesAsync();

			return userSensor;
		}

		public async Task<UserSensor> RestoreUserSensor(string userSensorId)
		{
			Validator.ValidateNull(userSensorId, "User sensor Id cannot be null!");
			Validator.ValidateGuid(userSensorId, "User sensor id is not in the correct format.Unable to parse to Guid!");

			UserSensor userSensor = await this.dataContext.UserSensors
				.Include(us => us.Sensor)
				.FirstAsync(us => us.Id.Equals(userSensorId));

			if (userSensorId == null)
			{
				throw new EntityNotFoundException();
			}

			userSensor.IsDeleted = false;
			userSensor.DeletedOn = null;

			await this.dataContext.SaveChangesAsync();

			return userSensor;
		}
	}
}
