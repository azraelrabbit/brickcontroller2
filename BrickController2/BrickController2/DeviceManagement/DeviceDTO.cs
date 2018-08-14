﻿using SQLite;

namespace BrickController2.DeviceManagement
{
    [Table("Device")]
    public class DeviceDTO
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Name { get; set; }
        public string DeviceId { get; set; }
        public string DeviceSpecificData { get; set; }
    }
}
