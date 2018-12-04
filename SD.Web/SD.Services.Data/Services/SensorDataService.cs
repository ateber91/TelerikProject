﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SD.Data.Context;
using SD.Data.Models.DomainModels;
using SD.Services.Data.Services.Contracts;
using SD.Services.External;
using X.PagedList;

namespace SD.Services.Data.Services
{
	public class SensorDataService : ISensorDataService
	{
		private readonly IApiClient apiClient;
		private readonly DataContext dataContext;
		private readonly INotificationService notificationService;

		public SensorDataService(IApiClient aPIClient, DataContext dataContext, INotificationService notificationService)
		{
			this.apiClient = aPIClient ?? throw new ArgumentNullException(nameof(aPIClient));
			this.dataContext = dataContext ?? throw new ArgumentNullException(nameof(dataContext));
			this.notificationService = notificationService;
		}

		//TODO: Handle exception coming from API. Catch and translate to business exception.
		//TODO: Then throw/bubble up.
		public async Task GetSensorsData()
		{
			IList<Sensor> allSensors = await this.dataContext.Sensors.ToListAsync();
			IList<SensorData> deleteList = new List<SensorData>();
			IList<SensorData> addList = new List<SensorData>();
			IList<Sensor> updateList = new List<Sensor>();

			IDictionary<Sensor, SensorData> sensorsDictionary = new Dictionary<Sensor, SensorData>();
			IList<Notification> notiList = new List<Notification>();

			foreach (var sensor in allSensors)
			{
				var lastTimeStamp = sensor.LastTimeStamp;
				TimeSpan difference = DateTime.Now.Subtract((DateTime)lastTimeStamp);
				var pollingInterval = sensor.MinPollingIntervalInSeconds;

				if (difference.TotalSeconds >= pollingInterval)
				{
					SensorData oldSensorData = await this.dataContext.SensorData
					.Include(sd => sd.Sensor.UserSensors)
					.Where(oSD => oSD.SensorId.Equals(sensor.SensorId))
					.OrderByDescending(oSD => oSD.TimeStamp)
					.FirstAsync();

					SensorData newSensorData = await this.apiClient
					.GetSensorData("sensorId?=" + sensor.SensorId);
					newSensorData.SensorId = sensor.SensorId;
					if (newSensorData.Value.Equals("true")) { newSensorData.Value = "1"; };
					if (newSensorData.Value.Equals("false")) { newSensorData.Value = "0"; };
					
					sensor.LastTimeStamp = newSensorData.TimeStamp;
					sensor.LastValue = newSensorData.Value;

					sensorsDictionary.Add(sensor, newSensorData);
					//await CheckForAlarm(sensor, newSensorData);

					addList.Add(newSensorData);
					deleteList.Add(oldSensorData);
					updateList.Add(sensor);

				}
			}

			notiList = await CheckForAlarmNotifications(sensorsDictionary);
			await this.dataContext.AddRangeAsync(notiList);

			await this.dataContext.AddRangeAsync(addList);
			this.dataContext.SensorData.RemoveRange(deleteList);
			this.dataContext.UpdateRange(updateList);

			await this.dataContext.SaveChangesAsync(false);
		}

		private async Task<IList<Notification>> CheckForAlarmNotifications(IDictionary<Sensor, SensorData> sensorsDictionary)
		{
			IList<Notification> notiList = new List<Notification>();

			foreach (var kvp in sensorsDictionary)
			{
				var newValue = double.Parse(kvp.Value.Value);

				var currentUserSensors = await kvp.Key.UserSensors.ToListAsync();
				foreach (var userSensor in currentUserSensors)
				{
					if ((newValue <= userSensor.AlarmMin || newValue >= userSensor.AlarmMax) && userSensor.AlarmTriggered == true)
					{
						var userId = userSensor.UserId.ToString();
						var message = userSensor.Name + " pinged, something is happening!";
						await this.notificationService.SendNotificationAsync(message, userId);

						notiList.Add(await this.CreateNotificationAsync(userId, message));
					}
				}
			}

			return notiList;


			//var newValue = double.Parse(newSensorData.Value);
			
			//var currentUserSensors = await sensor.UserSensors.ToListAsync();
			//foreach (var userSensor in currentUserSensors)
			//{
			//	if ((newValue <= userSensor.AlarmMin || newValue >= userSensor.AlarmMax) && userSensor.AlarmTriggered == true)
			//	{
			//		var userId = userSensor.UserId.ToString();
			//		var message = userSensor.Name + " pinged, something is happening!";
			//		await this.notificationService.SendNotificationAsync(message, userId);

			//		await this.AddNotificationAsync(userId, message);
			//	}
			//}
		}

		private async Task<Notification> CreateNotificationAsync(string userId, string message)
		{
			Notification noti = new Notification
			{
				UserId = Guid.Parse(userId),
				Message = message
			};

			return noti;
		}
	}
}
