﻿using NASDataBaseAPI.Server.Data.DataBaseSettings;
using NASDataBaseAPI.Server.Data.Safety;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NASAPITests.DataBaseTests.Init.Column_2
{
    public class CreateDataBase_2_ColumnAnd_1000_AddData__SafeMode
    {
        [Fact]
        public void Test_SettingsFile()
        {
            int ColumnCount = 2;
            int DataCount = 1000;
            int InClusters = 100;

            Directory.Delete("D:\\BMTest1", true);

            string[] datas = { "Tom", "Bob", "Tad" };

            DataBaseManager DBM = new DataBaseManager();

            var DB = DBM.CreateDataBase(new DataBaseSettings("BMTest1", "D:\\", SimpleEncryptor.GenerateRandomKey(128)
             , (uint)ColumnCount, CountBucketsInSector: (uint)InClusters));

            Random rnd = new Random();

            for (int i = 0; i < DataCount; i++)
            {
                DB.AddData(new string[2] { datas[rnd.Next(3)], datas[rnd.Next(3)] });
            }

            Assert.True(File.Exists("D:\\BMTest1\\Settings\\Settings.txt"));
        }

        [Fact]
        public void Test_CountBucketsInSector()
        {
            int ColumnCount = 2;
            int DataCount = 1000;
            int InClusters = 100;

            Directory.Delete("D:\\BMTest1", true);

            string[] datas = { "Tom", "Bob", "Tad" };

            DataBaseManager DBM = new DataBaseManager();

            var DB = DBM.CreateDataBase(new DataBaseSettings("BMTest1", "D:\\", SimpleEncryptor.GenerateRandomKey(128)
             , (uint)ColumnCount, CountBucketsInSector: (uint)InClusters));

            Random rnd = new Random();

            for (int i = 0; i < DataCount; i++)
            {
                DB.AddData(new string[2] { datas[rnd.Next(3)],datas[rnd.Next(3)] });
            }

            Assert.True(DB.Settings.CountBucketsInSector == (uint)InClusters, $"InClusters = {InClusters}|CountBucketsInSector = {DB.Settings.CountBucketsInSector}");
        }

        [Fact]
        public void Test_CountBuckets()
        {
            int ColumnCount = 2;
            int DataCount = 1000;
            int InClusters = 100;

            Directory.Delete("D:\\BMTest1", true);

            string[] datas = { "Tom", "Bob", "Tad" };

            DataBaseManager DBM = new DataBaseManager();

            var DB = DBM.CreateDataBase(new DataBaseSettings("BMTest1", "D:\\", SimpleEncryptor.GenerateRandomKey(128)
             , (uint)ColumnCount, CountBucketsInSector: (uint)InClusters));

            Random rnd = new Random();

            for (int i = 0; i < DataCount; i++)
            {
                DB.AddData(new string[2] { datas[rnd.Next(3)], datas[rnd.Next(3)] });
            }

            Assert.True(DB.Settings.CountBuckets == DataCount);
        }

        [Fact]
        public void Test_CountClusters()
        {
            int ColumnCount = 2;
            int DataCount = 1000;
            int ClustersCount = 10;
            int InClusters = 100;

            Directory.Delete("D:\\BMTest1", true);

            string[] datas = { "Tom", "Bob", "Tad" };

            DataBaseManager DBM = new DataBaseManager();

            var DB = DBM.CreateDataBase(new DataBaseSettings("BMTest1", "D:\\", SimpleEncryptor.GenerateRandomKey(128)
             , (uint)ColumnCount, CountBucketsInSector: (uint)InClusters));

            Random rnd = new Random();

            for (int i = 0; i < DataCount; i++)
            {
                DB.AddData(new string[2] { datas[rnd.Next(3)], datas[rnd.Next(3)] });
            }

            Assert.True(DB.Settings.CountClusters == ClustersCount, $"ClustersCount = {ClustersCount}|DB.settings.CountClusters = {DB.Settings.CountClusters}");
        }

        [Fact]
        public void Test_ColumnCount()
        {
            int ColumnCount = 2;
            int DataCount = 1000;
            int InClusters = 100;

            Directory.Delete("D:\\BMTest1", true);

            string[] datas = { "Tom", "Bob", "Tad" };

            DataBaseManager DBM = new DataBaseManager();

            var DB = DBM.CreateDataBase(new DataBaseSettings("BMTest1", "D:\\", SimpleEncryptor.GenerateRandomKey(128)
             , (uint)ColumnCount, CountBucketsInSector: (uint)InClusters));

            Random rnd = new Random();

            for (int i = 0; i < DataCount; i++)
            {
                DB.AddData(new string[2] { datas[rnd.Next(3)], datas[rnd.Next(3)] });
            }

            Assert.True(DB.Columns.Count == ColumnCount);
        }
    }
}