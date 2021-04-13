﻿using System;
using System.Collections.Generic;
using System.Data.SQLite;
using Metrics.Requests;
using MetricsManager.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Asp.net_2.Controllers
{
    [Route("api/metrics/cpu")]
    [ApiController]
    public class CpuMetricsController : ControllerBase
    {
        private readonly ILogger<CpuMetricsController> _logger;
        private ICpuMetricsRepository repository;
        public CpuMetricsController(ICpuMetricsRepository repository, ILogger<CpuMetricsController> logger)
        {
            this.repository = repository;
            _logger = logger;
            _logger.LogDebug(1, "NLog встроен в CpuMetricsController");
        }

        [HttpPost("create")]
        public IActionResult Create([FromBody] CpuMetricCreateRequest request)
        {
            repository.Create(new CpuMetrics
            {
                Time = request.Time,
                Value = request.Value
            });
            return Ok();
        }
        [HttpGet("all")]
        public IActionResult GetAll()
        {
            var metrics = repository.GetAll();
            var response = new AllCpuMetricsResponse()
            {
                Metrics = new List<CpuMetricDto>()
            };
            foreach (var metric in metrics)
            {
                response.Metrics.Add(new CpuMetricDto
                {
                    Time = metric.Time,
                    Value =
                metric.Value,
                    Id = metric.Id
                });
            }
            return Ok(response);
        }
        [HttpGet("sql-read-write-test")]
        public IActionResult TryToInsertAndRead()
        {
            // Создаем строку подключения в виде базы данных в оперативной памяти
            string connectionString = "Data Source=:memory:";
            // создаем соединение с базой данных
            using (var connection = new SQLiteConnection(connectionString))
            {
                // открываем соединение
                connection.Open();
                // создаем объект через который будут выполняться команды к базе данных
                using (var command = new SQLiteCommand(connection))
                {
                    // задаем новый текст команды для выполнения
                    // удаляем таблицу с метриками если она существует в базе данных
                    command.CommandText = "DROP TABLE IF EXISTS cpumetrics";
                    // отправляем запрос в базу данных
                    command.ExecuteNonQuery();
                    // создаем таблицу с метриками
                    command.CommandText = @"CREATE TABLE cpumetrics(id INTEGER PRIMARY KEY, value INT, time INT)";
                    command.ExecuteNonQuery();
                    // создаем запрос на вставку данных
                    command.CommandText = "INSERT INTO cpumetrics(value, time)VALUES(10, 1)";
                    command.ExecuteNonQuery();
                    command.CommandText = "INSERT INTO cpumetrics(value, time)VALUES(50, 2)";
                    command.ExecuteNonQuery();
                    command.CommandText = "INSERT INTO cpumetrics(value, time)VALUES(75, 4)";
                    command.ExecuteNonQuery();
                    command.CommandText = "INSERT INTO cpumetrics(value, time)VALUES(90, 5)";
                    command.ExecuteNonQuery();
                    // создаем строку для выборки данных из базы
                    // LIMIT 3 обозначает, что мы достанем только 3 записи
                    string readQuery = "SELECT * FROM cpumetrics LIMIT 3";
                    // создаем массив, в который запишем объекты с данными из базы данных
                    var returnArray = new CpuMetricDto[3];
                    // изменяем текст команды на наш запрос чтения
                    command.CommandText = readQuery;
                    // создаем читалку из базы данных
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        // счетчик для того, чтобы записать объект в правильное место в массиве
                        var counter = 0;
                        // цикл будет выполняться до тех пор, пока есть что читать из базы данных
                        while (reader.Read())
                        {
                            // создаем объект и записываем его в массив
                            returnArray[counter] = new CpuMetricDto
                            {
                                Id = reader.GetInt32(0), // читаем данные полученные из базы данных
                                Value = reader.GetInt32(0), // преобразуя к целочисленному типу
                                Time = TimeSpan.Parse(reader.GetInt32(0).ToString())
                            };
                            // увеличиваем значение счетчика
                            counter++;
                        }
                    }
                    // оборачиваем массив с данными в объект ответа и возвращаем пользователю
                    return Ok(returnArray);
                }
            }
        }

        [HttpGet("sql-test")]
        public IActionResult TryToSqlLite()
        {
            string cs = "Data Source=:memory:";
            string stm = "SELECT SQLITE_VERSION()";
            using (var con = new SQLiteConnection(cs))
            {
                con.Open();
                using var cmd = new SQLiteCommand(stm, con);
                string version = cmd.ExecuteScalar().ToString();
                return Ok(version);
            }
        }

        [HttpGet("agent/{agentId}/from/{fromTime}/to/{toTime}")]
        public IActionResult GetMetricsFromAgent([FromRoute] int agentId, [FromRoute]
        TimeSpan fromTime, [FromRoute] TimeSpan toTime)
        {
            _logger.LogInformation("GetMetricsFromAgent");
            return Ok();
        }


        [HttpGet("agent/{agentId}/from/{fromTime}/to/{toTime}/percentiles/{percentile}")]
        public IActionResult GetMetricsByPercentileFromAgent([FromRoute] int agentId,
        [FromRoute] TimeSpan fromTime, [FromRoute] TimeSpan toTime)
        {
            _logger.LogInformation("GetMetricsByPercentileFromAgent");
            return Ok();
        }

        [HttpGet("cluster/from/{fromTime}/to/{toTime}")]
        public IActionResult GetMetricsFromAllCluster([FromRoute] TimeSpan fromTime,
        [FromRoute] TimeSpan toTime)
        {
            _logger.LogInformation("GetMetricsFromAllCluster");
            return Ok();
        }
        [HttpGet("cluster/from/{fromTime}/to/{toTime}/percentiles/{percentile}")]
        public IActionResult GetMetricsByPercentileFromAllCluster([FromRoute]
        TimeSpan fromTime, [FromRoute] TimeSpan toTime, [FromRoute] Percentile percentile)
        {
            _logger.LogInformation("GetMetricsByPercentileFromAllCluster");
            return Ok();
        }
    }
   
}

namespace MetricsManager.Enums
{
    public enum Percentile
    {
        Median = 0,
        P75 = 1,
        P90 = 2,
        P95 = 3,
        P99 = 4
    }
}


