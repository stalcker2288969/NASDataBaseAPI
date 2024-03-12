﻿using NASDatabase.Data;
using NASDatabase.Data.DataTypesInColumn;
using NASDatabase.Interfaces;
using NASDatabase.Server.Data.DatabaseSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NASDatabase.Server.Data
{
    public class Database : IDisposable
    {
        #region Events
        /// <summary>
        /// [data]
        /// </summary>
        public Action<string[]> _RemoveDataByData;
        /// <summary>
        /// [data, id]
        /// </summary>
        public Action<string[], int> _RemoveData;
        /// <summary>
        /// [data, id] Добавить данные 
        /// </summary>
        public Action<string[], int> _AddData;
        /// <summary>
        /// [name] Добавляет столбец
        /// </summary>
        public Action<string> _AddColumn;
        /// <summary>
        /// [name] Удаление столбца 
        /// </summary>
        public Action<string> _RemoveColumn;
        /// <summary>
        /// [number] !Часто вызывается в сложных функциях и сложной логикой не стоит наделять! 
        /// </summary>
        public Action<int> _LoadedNewSector;
        /// <summary>
        /// [left, right] Произошло копирование одного столбца в другой
        /// </summary>
        public Action<string, string> _CloneColumn;
        /// <summary>
        /// [name] [sector]
        /// </summary>
        public Action<string, int> _ClearAllColumn;

        public Action _ClearAllBase;
        /// <summary>
        /// [oldName] [newName]
        /// </summary>
        public Action<string, string> _RenameColumn;
        /// <summary>
        /// [columnName] [itemData]
        /// </summary>
        public Action<string, ItemData> _SetDataInColumn;
        #endregion

        #region Exeption
        private const string ExeptionThereIsNotColumn = "Не был обнаружен данный столбец!";
        private const string ExeptionLengthReceivedDataDoesNotMatchNumberOfColumns = "Длина поступивших данных не совпадает c количество столбцов  ";
        private const string ExeptionTheParametersDoNotMatchInQuantity = "Параметры не совпадают по количеству!";
        #endregion

        public List<AColumn> Columns { get; protected set; }

        public List<uint> FreeIDs { get; protected set; } = new List<uint>();

        public DatabaseSettings.DatabaseSettings Settings;

        public IDataBaseSaver<AColumn> DataBaseSaver;
        public IDataBaseReplayser DataBaseReplayser;
        public IDataBaseLoader<AColumn> DataBaseLoader;
        public ILoger DataBaseLoger;

        public DatabaseServer Server;

        private DatabaseManager _myManager;

        public uint LoadedSector { get; private set; } = 1;

        private StringBuilder _ForPrint = new StringBuilder();

        #region Конструкторы

        public Database(int countColumn, DatabaseSettings.DatabaseSettings settings, int loadedSector = 1)
        {
            Columns = new List<AColumn>();
            this.Settings = settings;
            SetLoadedSector((int)loadedSector);
            for (int i = 0; i < countColumn; i++)
            {
                Columns.Add((AColumn)new Column(i.ToString()));
            }
        }

        public Database(List<AColumn> Column, DatabaseSettings.DatabaseSettings settings, int loadedSector = 1)
        {
            Columns = Column;
            this.Settings = settings;
            SetLoadedSector((int)loadedSector);
        }

        /// <summary>
        /// Изменяет тип работы сохранения данных на безопасный
        /// </summary>
        public virtual void EnableSafeMode()
        {
            lock (this)
            {
                if (Settings.SaveMod != true)
                {
                    Settings = new DatabaseSettings.DatabaseSettings(Settings, true);
                }

                DataBaseSaver = _myManager._databaseSavers[Convert.ToInt32(true)];
                DataBaseLoader = _myManager._databaseSavers[(int)Convert.ToInt32(true)];
                DataBaseReplayser = _myManager._databaseSavers[((int)Convert.ToInt32(true))];
            }
        }
        /// <summary>
        /// Изменяет тип мода сохранения данных на не безопасный
        /// </summary>
        public virtual void DisableSafeMode()
        {
            lock (this)
            {
                if (Settings.SaveMod != false)
                {
                    Settings = new DatabaseSettings.DatabaseSettings(Settings, false);
                }
                DataBaseSaver = _myManager._databaseSavers[Convert.ToInt32(false)];
                DataBaseLoader = _myManager._databaseSavers[(int)Convert.ToInt32(false)];
                DataBaseReplayser = _myManager._databaseSavers[((int)Convert.ToInt32(false))];
            }
        }

        /// <summary>
        /// Вычисляет сектор к которому нужно обратиться чтобы получить данные по ID 
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        public uint GetSectorByID(uint ID)
        {
            return (ID / Settings.CountBucketsInSector) + 1;
        }
        /// <summary>
        /// Сеттер для LoadedSector, оповещает о изменение свойства
        /// </summary>
        /// <param name="NewSectorsID"></param>
        protected void SetLoadedSector(int NewSectorsID)
        {
            LoadedSector = (uint)NewSectorsID; 
            Settings.CountClusters = LoadedSector - 1 == Settings.CountClusters ? LoadedSector : Settings.CountClusters;
            _LoadedNewSector?.Invoke(NewSectorsID);
        }

        /// <summary>
        /// Загрузка класера/просто сокрщает код  
        /// </summary>
        /// <param name="NewSector"></param>
        /// <returns></returns>
        private void _LoadDataBase(int NewSector)
        {
            if (NewSector == 0)
                NewSector = 1;

            if (LoadedSector != NewSector)
            {
                Columns.Clear();
                Columns.AddRange((IEnumerable<AColumn>)DataBaseLoader.LoadCluster(Settings.Path, (uint)NewSector, Settings.Key));
                SetLoadedSector((int)NewSector);
            }
        }
        #endregion

        #region Глобальное взаимодействое
        /// <summary>
        /// Получает айдишики все строк по параметрам
        /// </summary>
        /// <param name="NameColumn"></param>
        /// <param name="Data"></param>
        /// <returns></returns>
        public virtual int[] GetAllIDsByParams(string NameColumn, string Data)
        {
            lock (Columns)
            {
                List<int> IDs = new List<int>();

                for (int i = 0; i < Settings.CountClusters; i++)
                {
                    _LoadDataBase(i);

                    IDs.AddRange(this[NameColumn].FindIDs(Data) ?? new int[0]);
                }

                return IDs.ToArray();
            }
        }

        public virtual int[] GetAllIDsByParams(int NumberColumn, string Data)
        {
            return GetAllIDsByParams(Columns[NumberColumn].Name, Data);
        }
        /// <summary>
        /// Изменяет тип в указанном столбце 
        /// </summary>
        /// <param name="NameColumn"></param>
        /// <param name="TypeOfData"></param>
        public virtual void ChangTypeInColumn(string NameColumn, TypeOfData TypeOfData)
        {
            lock (Columns)
            {
                for(int i = 0; i < Settings.CountClusters; i++)
                {
                    _LoadDataBase(i);
                    foreach (var column in Columns)
                    {
                        column.ChangType(TypeOfData);
                    }
                    DataBaseSaver.SaveAllCluster(Settings, LoadedSector, Columns.ToArray());
                }     
            }
        }

        public void ChangTypeInColumn(AColumn Column, TypeOfData DataType)
        {
            ChengTypeInColumn(Column.Name, DataType);
        }

        public void ChangTypeInColumn(int column, TypeOfData DataType)
        {
            ChengTypeInColumn(Columns[column].Name, DataType);
        }
        /// <summary>
        /// Удаляет столбец
        /// </summary>
        /// <param name="ColumnName"></param>
        public virtual void RemoveColumn(string ColumnName)
        {
            lock (Columns)
            {
                for (int i = 0; i < Settings.CountClusters; i++)
                {
                    _LoadDataBase(i);
                    this[ColumnName].ClearBoxes();

                    DataBaseSaver.SaveAllCluster(Settings, (uint)i, Columns.ToArray());
                }

                Settings.ColumnsCount -= 1;
                _RemoveColumn?.Invoke(ColumnName);
            }
        }

        public virtual void RemoveColumn(int NumberOFColumn)
        {
            RemoveColumn(Columns[NumberOFColumn].Name);
        }

        public virtual void RemoveColumn(Interfaces.AColumn ColumnName)
        {
            RemoveColumn(ColumnName.Name);
        }

        /// <summary>
        /// Добавляет новый столбец. Процедура очень не продуктивная
        /// </summary>
        /// <param name="Name"></param>
        public virtual void AddColumn(string Name)
        {
            lock (Columns)
            {
                Settings.ColumnsCount += 1;
                for (int i = 0; i < Settings.CountClusters; i++)
                {
                    _LoadDataBase(i);

                    Column column = new Column(Name, Columns[0].Offset);//Новый столбец

                    ItemData[] itemDatas = new ItemData[Columns[0].GetCounts()];

                    for (int g = 0; g < itemDatas.Length; g++)//связывает ивенд дестроя и столбец
                    {
                        itemDatas[g] = new ItemData(g, " ");
                    }

                    column.SetDatas(itemDatas); //записываем пустые ячейки в новый столбец

                    Columns.Add(column);
                    DataBaseSaver.SaveAllCluster(Settings, (uint)i, Columns.ToArray());
                }
               
                _AddColumn?.Invoke(Name);
            }
        }

        /// <summary>
        /// Добавляет столбик и задет тип данных в столбике
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="dataType"></param>
        public virtual void AddColumn(string Name, TypeOfData dataType)
        {
            lock (Columns)
            {
                Settings.ColumnsCount += 1;
                for (int i = 0; i < Settings.CountClusters; i++)
                {
                    _LoadDataBase(i);

                    Column column = new Column(Name, dataType, Columns[0].Offset);//Новый столбец

                    ItemData[] itemDatas = new ItemData[Columns[0].GetCounts()];

                    for (int g = 0; g < itemDatas.Length; g++)//связывает ивенд дестроя и столбец
                    {
                        itemDatas[g] = new ItemData(g, " ");
                    }

                    column.SetDatas(itemDatas); //записываем пустые ячейки данные в новый столбец

                    Columns.Add(column);
                    DataBaseSaver.SaveAllCluster(Settings, (uint)i, Columns.ToArray());
                }               
                _AddColumn?.Invoke(Name);
            }
        }

        /// <summary>
        /// Добавляет столбик и задет тип данных в столбике
        /// </summary>
        public virtual void AddColumn(AColumn Column)
        {
            AddColumn(Column.Name, Column.TypeOfData);
        }

        /// <summary>
        /// Копирует данные из левого столбца в правый. Важно, что копирует внутри самой базы данных
        /// </summary>
        /// <param name="LeftColumn"></param>
        /// <param name="RightColumn"></param>
        public virtual void CloneTo(AColumn LeftColumn, AColumn RightColumn)
        {
            lock (Columns)
            {
                var leftName = LeftColumn.Name;
                var rightName = RightColumn.Name;

                if (this[leftName].TypeOfData != this[rightName].TypeOfData)
                {
                    RightColumn.ChangType(RightColumn.TypeOfData);
                    DataBaseSaver.SaveAllCluster(Settings, LoadedSector, Columns.ToArray());
                }

                for (int i = 1; i < Settings.CountClusters; i++)
                {
                    _LoadDataBase(i);
                    ItemData[] itemDatas = this[leftName].GetDatas();
                    this[rightName].SetDatas(itemDatas);
                    DataBaseSaver.SaveAllCluster(Settings, (uint)i, Columns.ToArray());
                }                
                _CloneColumn?.Invoke(leftName, rightName);
            }
        }

        /// <summary>
        /// Копирует данные из левого столбца в правый. Важно, что копирует внутри самой базы данных 
        /// </summary>
        /// <param name="LeftColumn"></param>
        /// <param name="RigthColumn"></param>
        public virtual void CloneTo(string LeftColumn, string RigthColumn)
        {
            lock (Columns)
            {
                var rightColumn = this[RigthColumn];
                var leftColumn = this[LeftColumn];

                if (leftColumn.TypeOfData != rightColumn.TypeOfData)
                {
                    rightColumn.ChangType(leftColumn.TypeOfData);
                    DataBaseSaver.SaveAllCluster(Settings, LoadedSector, Columns.ToArray());
                }

                for (int i = 1; i < Settings.CountClusters; i++)
                {
                    _LoadDataBase(i);
                    ItemData[] itemDatas = this[LeftColumn].GetDatas();
                    this[RigthColumn].SetDatas(itemDatas);
                    DataBaseSaver.SaveAllCluster(Settings, (uint)i, Columns.ToArray());
                }
                _CloneColumn?.Invoke(LeftColumn, RigthColumn);
            }
        }

        /// <summary>
        /// Отчищает отдельный столбец в указанном секторе/кластере или везде 
        /// </summary>
        /// <param name="Column"></param>
        public virtual void ClearAllColumn(AColumn Column, int InSector = -1)
        {
            if (InSector == -1)
            {
                for (int i = 1; i < Settings.CountClusters; i++)
                {
                    var _column = this[Column.Name];
                    if (_column.TypeOfData == Column.TypeOfData)
                    {
                        _LoadDataBase(i);
                        _column.ClearBoxes();
                        DataBaseSaver.SaveAllCluster(Settings, (uint)i, Columns.ToArray());                       
                    }
                }
            }
            else
            {
                var _column = this[Column.Name];
                if (_column.TypeOfData == Column.TypeOfData)
                {
                    _LoadDataBase(InSector);
                    _column.ClearBoxes();
                    DataBaseSaver.SaveAllCluster(Settings, (uint)InSector, Columns.ToArray());
                }
            }
        }

        /// <summary>
        /// Отчищает отдельный столбец в указаном секторе/класторе или везде 
        /// </summary>
        /// <param name="ColumnName"></param>
        public virtual void ClearAllColumn(string ColumnName, int InSector = -1)
        {
            if (InSector == -1)
            {
                for (int i = 1; i < Settings.CountClusters; i++)
                {
                    var _column = this[ColumnName];
                    _LoadDataBase(i);
                    _column.ClearBoxes();
                    DataBaseSaver.SaveAllCluster(Settings, (uint)i, Columns.ToArray());
                }
            }
            else
            {
                var _column = this[ColumnName];
                _LoadDataBase(InSector);
                _column.ClearBoxes();
                DataBaseSaver.SaveAllCluster(Settings, (uint)InSector, Columns.ToArray());
            }
        }
        /// <summary>
        /// Чистит всю базу / не производительная команда
        /// </summary>
        public virtual void ClearAllBase()
        {
            lock (Columns)
            {
                for (int i = 1; i < Settings.CountClusters; i++)
                {
                    _LoadDataBase(i);
                    foreach (Interfaces.AColumn t in Columns)
                    {
                        t.ClearBoxes();
                    }
                    DataBaseSaver.SaveAllCluster(Settings, (uint)i, Columns.ToArray());
                }
                Settings.CountBuckets = 0;
            }
        }

        public virtual void RenameColumn(string name, string newName)
        {
            lock (Columns)
            {
                this[name].Name = newName;
                _myManager.SaveStatesDatabase(this);
            }
        }

        public virtual void RenameColumn(int name, string newName)
        {
            RenameColumn(Columns[name].Name, newName);
        }

        public virtual void RenameColumn(AColumn Column, string newName)
        {
            RenameColumn(Column.Name, newName);
        }

        #endregion

        #region Добавление/замена данных 

        /// <summary>
        /// Заменяет все элементы в указанном столбце на новые данные, довольно тяжелая операция 
        /// </summary>
        /// <param name="Params"></param>
        /// <param name="New"></param>
        /// <param name="SectorID"></param>
        /// <param name="ColumnName"></param>
        public virtual void ChangeEverythingTo(string ColumnName, string Params, string New, int SectorID = -1)
        {
            lock (Columns)
            {
                if (SectorID == -1)
                {
                    for (int i = 1; i < Settings.CountClusters; i++)
                    {
                        _LoadAndChengeDataInCluster(i, ColumnName, Params, New);
                    }
                }
                else
                {
                    _LoadAndChengeDataInCluster((int)SectorID, ColumnName, Params, New);
                }
            }
        }

        /// <summary>
        /// Приватынй метод для ChangeEverythingTo
        /// </summary>
        /// <param name="Sector"></param>
        /// <param name="ColumnName"></param>
        /// <param name="Params"></param>
        /// <param name="New"></param>
        private void _LoadAndChengeDataInCluster(int Sector, string ColumnName, string Params, string New)
        {sector
            _LoadDataBase(Sector);
            foreach (Interfaces.AColumn t in Columns)
            {
                if (t.Name == ColumnName)
                {
                    var ids = t.FindIDs(Params);
                    foreach (var id in ids)
                    {
                        var itemData = new ItemData(id, New);
                        t.SetDataByID(itemData);
                        _SetDataInColumn?.Invoke(ColumnName, itemData);
                    }
                }
            }
            DataBaseSaver.SaveAllCluster(Settings, (uint)Sector, Columns.ToArray());
        }


        /// <summary>
        /// Заменяет строку
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="Data"></param>
        public virtual void SetData(int ID, params string[] Data)
        {
            uint SectorID = GetSectorByID((uint)ID);

            ReplayesDataBySectorAndID(SectorID, ID, Data);
        }

        /// <summary>
        /// Заменяет строку c помошью DataLine 
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="datas"></param>
        public virtual void SetData(IDataRow Row)
        {
            SetData(Row.ID, Row.GetData());
        }

        public virtual void SetData<T>(T Row) where T : IDataRow
        {
            SetData(Row.ID, Row.GetData());
        }
        /// <summary>
        /// Создает экземпляр IDataRow и заполняет им строчку
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ID"></param>
        public virtual void SetData<T>(int ID) where T : IDataRow, new()
        {
            var t = Activator.CreateInstance<T>();
            SetData(ID, t.GetData());
        }

        /// <summary>
        /// Заменяет строку
        /// </summary>
        /// <param name="Data"></param>
        public virtual void SetData(params ItemData[] Data)
        {
            List<string> _datas = new List<string>();

            foreach (var d in Data)
            {
                _datas.Add(d.Data);
            }

            SetData(Data[0].ID, _datas.ToArray());
        }

        /// <summary>
        /// В необходимой табличке происходит замена данных
        /// </summary>
        public virtual void SetDataInColumn(string ColumnName, int ID, string NewData)
        {
            SetDataInColumn(ColumnName, new ItemData(ID, NewData));           
        }
        /// <summary>
        /// В необходимой табличке происходит замена данных в NewItemData укажите новые данные и id ячейки в которой нужно перезаписать данные
        /// </summary>
        /// <param name="ColumnName"></param>
        /// <param name="NewItemData"></param>
        public virtual void SetDataInColumn(string ColumnName, ItemData NewItemData)
        {
            lock (Columns)
            {
                uint SectorID = GetSectorByID((uint)NewItemData.ID);

                _LoadDataBase((int)SectorID);

                this[ColumnName].SetDataByID(NewItemData);
               
                DataBaseSaver.SaveAllCluster(Settings, SectorID, Columns.ToArray());
                _SetDataInColumn?.Invoke(ColumnName, NewItemData);
            }
        }

        public virtual void SetDataInColumn(AColumn Column, ItemData NewItemData)
        {
            SetDataInColumn(Column.Name, NewItemData);
        }

        public virtual void SetDataInColumn(AColumn Column, int ID, string NewData)
        {
            SetDataInColumn(Column.Name, new ItemData(ID, NewData));
        }
        /// <summary>
        /// Добавляет данные в таблицу, важно чтобы длина поступающего массива была равна кол-ву столбцов  
        /// Ошибки: Exception($"Длина поступивших данных меньше кол-ва столбцов: {Columns.Count}")
        /// </summary>
        /// <param name="datas"></param>
        public virtual void AddData(params string[] datas)
        {
            if (datas?.Length == Columns.Count)
            {
                if (FreeIDs.Count == 0)
                {
                    uint SectorID = GetSectorByID(Settings.CountBuckets);//Опредиляем к какому сектору обратиться
                    AddBySectorAndID(SectorID, (int)Settings.CountBuckets, datas);
                }
                else
                {
                    uint FreeID = FreeIDs[0];
                    FreeIDs.Remove(FreeID);
                    uint SectorID = GetSectorByID(FreeID);
                    ReplayesDataBySectorAndID(SectorID, (int)FreeID, datas);
                }

            }
            else if (datas?.Length < Columns.Count)
            {
                throw new Exception(ExeptionLengthReceivedDataDoesNotMatchNumberOfColumns + Columns.Count);
            }
            else if (datas?.Length > Columns.Count)
            {
                throw new Exception(ExeptionLengthReceivedDataDoesNotMatchNumberOfColumns + Columns.Count);
            }
        }

        /// <summary>
        /// Добавляет данные в таблицу, важно чтобы длина поступающего массива была равна кол-ву столбцов  
        /// Ошибки: Exception($"Длина поступивших данных меньше кол-ва столбцов: {Columns.Count}")
        /// </summary>
        /// <param name="datas"></param>
        public virtual void AddData(IDataRow Row)
        {
            AddData(Row.GetData());
        }

        /// <summary>
        /// Добавляет данные в таблицу, важно чтобы длина поступающего массива была равна кол-ву столбцов  
        /// Ошибки: Exception($"Длина поступивших данных меньше кол-ва столбцов: {Columns.Count}")
        /// </summary>
        /// <param name="data"></param>
        public virtual void AddData(params object[] data)
        {
            List<string> strings = new List<string>();

            foreach (var d in data)
            {
                strings.Add(d.ToString());
            }

            AddData(strings.ToArray());
        }

        private void ReplayesDataBySectorAndID(uint SectorID, int ID, string[] Data)
        {
            lock (Columns)
            {
                _LoadDataBase((int)SectorID);
                bool res = false;

                List<ItemData> itemDatas = new List<ItemData>();
                for (int i = 0; i < this.Columns.Count; i++)
                {
                    res = Columns[i].SetDataByID(new ItemData(ID, Data[i]));
                    itemDatas.Add(new ItemData(ID, Data[i]));
                }

                Settings.CountBuckets += 1;
                DataBaseReplayser.ReplayesElement(Settings, SectorID, itemDatas.ToArray());
               
                
                if(res)
                    _AddData?.Invoke(Data, ID);
            }
        }
        /// <summary>
        /// Добавляет данные по сектору и id
        /// </summary>
        /// <param name="SectorID"></param>
        /// <param name="Data"></param>
        private void AddBySectorAndID(uint SectorID, int ID, string[] Data)
        {
            lock (Columns)
            {
                _LoadDataBase((int)SectorID);

                List<ItemData> itemDatas = new List<ItemData>();
                for (int i = 0; i < this.Columns.Count; i++)
                {
                    itemDatas.Add(new ItemData(ID, Data[i]));
                    Columns[i].Push(Data[i], (uint)ID);
                }
                Settings.CountBuckets += 1;
                DataBaseSaver.AddElement(Settings, SectorID, itemDatas.ToArray());
                

                _AddData?.Invoke(Data, ID);
            }
        }
        #endregion

        #region Удаление данных
        /// <summary>
        /// Удаляет данные по введенному ID
        /// </summary>
        /// <param name="ID"></param>
        public virtual void RemoveDataByID(int ID)
        {
            lock (Columns)
            {
                uint SectorID = GetSectorByID((uint)ID);
                List<ItemData> ItemDatas = new List<ItemData>();

                var data = GetDataByID((int)ID);

                _LoadDataBase((int)SectorID);

                for (int i = 0; i < this.Columns.Count; i++)
                {
                    Columns[i].SetDataByID(new ItemData((int)ID, " "));
                    ItemDatas.Add(new ItemData((int)ID, " "));
                }
                FreeIDs.Add((uint)ID);
                DataBaseReplayser.ReplayesElement(Settings, SectorID, ItemDatas.ToArray());
                Settings.CountBuckets -= 1;
              

                _RemoveData?.Invoke(data, (int)ID);
            }
        }

        /// <summary>
        /// Удаляет все данные из базы подходящие по параметру.
        /// </summary>
        /// <param name="datas">Параметр</param>
        public virtual bool RemoveAllData(params string[] datas)
        {
            if (datas.Length == Columns.Count)
            {
                for (int i = 0; i < Settings.CountClusters; i++)
                {
                    _LoadDataBase(i);
                    List<bool> bools = new List<bool>();

                    for (int g = 0; g < this.Columns.Count; g++)
                    {
                        if (Columns[g].Pop(datas[g]))
                        {
                            bools.Add(true);
                        }
                    }

                    if (bools.Count == Columns.Count)
                    {
                        Settings.CountBuckets -= 1;
                        DataBaseSaver.SaveAllCluster(Settings, (uint)i, Columns.ToArray());

                        string _datas = "";//для логирования

                        foreach (var d in datas)
                        {
                            _datas += d;
                        }

                        return true;
                    }
                    else
                    {
                        DataBaseSaver.SaveAllCluster(Settings, (uint)i, Columns.ToArray());
                        return false;
                    }

                }
            }
            _RemoveDataByData?.Invoke(datas);
            return true;
        }

        /// <summary>
        /// Удаляет все данные из базы подходящие по параметру.
        /// </summary>
        public virtual bool RemoveAllData(IDatRows dataline)
        {
            return RemoveAllData(dataline.GetData());
        }

        /// <summary>
        /// Удаляет все данные из базы подходящие по параметру.
        /// </summary>
        public virtual bool RemoveAllData(params object[] datas)
        {
            List<string> strings = new List<string>();
            foreach (object data in datas)
            {
                strings.Add(data.ToString());
            }
            return RemoveAllData(strings);
        }
        /// <summary>
        /// Удаляет все данные по указанному списку ID
        /// </summary>
        /// <param name="IDs"></param>
        public virtual void RemoveDatasByIDs(int[] IDs)
        {
            foreach (var d in IDs)
            {
                RemoveDataByID(d);
            }
        }

        #endregion

        #region Сортировка/Получение данных по параметрам
        /// <summary>
        /// Отображает загруженный сектор в память. Если происходит ошибка возврат " " 
        /// </summary>
        /// <returns></returns>
        public virtual string PrintBase()
        {
            try
            {
                lock (Columns)
                {
                    lock (_ForPrint)
                    {
                        int l = Columns[0].GetCounts();

                        StringBuilder ColumnsBuilder = new StringBuilder();

                        ColumnsBuilder.Append("% Number % | ");
                        for (int g = 0; g < Columns.Count; g++)
                        {
                            ColumnsBuilder.Append("% " + Columns[g].Name + " % | ");
                        }

                        string CLText = ColumnsBuilder.ToString();
                        string lines = new string('-', CLText.Length);//Делаем линии длиной равной длине кол-ву столбцов

                        _ForPrint.Append($"Columns names:\n{lines}\n");
                        _ForPrint.Append(CLText);
                        _ForPrint.Append($"\n{lines}\n");

                        for (int id = 0; id < l; id++)
                        {
                            _ForPrint.Append(id.ToString() + " | ");
                            for (int g = 0; g < Columns.Count; g++)
                            {
                                _ForPrint.Append(Columns[g].FindDataByID(id) + " | ");
                            }

                            _ForPrint.Append($"\n{lines}\n");
                        }

                        string Text = _ForPrint.ToString();
                        _ForPrint.Clear();
                        return Text;
                    }
                }
            }
            catch
            {
                return " ";
            }
        }

        /// <summary>
        /// Сканирует всю БД в поисках подходящих строк 
        /// </summary>
        /// <param name="ColumnName"></param>
        /// <param name="Data"></param>
        /// <returns></returns>
        public virtual Row[] GetAllDataInBaseByColumnName(string ColumnName, string Data)
        {
            lock (Columns)
            {
                List<Row> Boxes = new List<Row>();

                for (int i = 1; i < Settings.CountClusters + 1; i++)
                {
                    _LoadDataBase(i);

                    int[] ids = this[ColumnName].FindIDs(Data);

                    for (int j = 0; j < ids.Length; j++)
                    {
                        Boxes.Add(new Row());
                        string[] strings = new string[Columns.Count];

                        for (int k = 0; k < Columns.Count; k++)
                        {
                            strings[k] = Columns[k].FindDataByID((int)ids[j]);
                        }
                        Boxes[j].Init(ids[j], strings);
                    }                  
                }
                return Boxes.ToArray();
            }
        }


        /// <summary>
        /// Сканирует всю БД в поисках подходящих строк 
        /// </summary>
        /// <param name="ColumnName"></param>
        /// <param name="Data"></param>
        /// <returns></returns>
        public virtual Row[] GetAllDataInBaseByColumnName(AColumn Column, string Data)
        {
            return GetAllDataInBaseByColumnName(Column.Name, Data);
        }

        /// <summary>
        /// По введенным параметрам ищет данные в БД
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Columns">Табличка параметр</param>
        /// <param name="SearchTypes">способ поиска данных</param>
        /// <param name="Params">Данные от которых нужно операться</param>
        /// <param name="InSectro">Сектор в котором нужно искать данные(-1 - во всех)</param>
        /// <returns></returns>
        public virtual T[] SmartSearch<T>(AColumn[] Columns, SearchType[] SearchTypes, string[] Params, int InSectro = -1)
            where T : IDatRows, new()
        {
            lock (this.Columns)
            {
                List<T> Boxes = new List<T>();
                List<List<int>> Search = new List<List<int>>();
                List<int> resultIDs = new List<int>();

                if (Columns.Length != SearchTypes.Length && Columns.Length != Params.Length)
                    throw new ArgumentException(ExeptionTheParametersDoNotMatchInQuantity);

                if (InSectro == -1)
                {
                    for (int i = 0; i < Settings.CountClusters; i++)
                    {
                        Boxes.AddRange(SmartSearch<T>(Columns, SearchTypes, Params, i));
                    }
                }
                else
                {
                    _LoadDataBase(InSectro);
                    for (int j = 0; j < Params.Length; j++)
                    {
                        var _colomn = this[Columns[j].Name];

                        if (_colomn.TypeOfData == Columns[j].TypeOfData)
                        {
                            List<int> IDs = new List<int>();
                            IDs = new SmartSearcher(Columns[j], _colomn, SearchTypes[j], Params[j]).Search();
                            Search.Add(IDs);
                        }
                    }

                    for (int i = 0; i < Search.Count; i++)
                    {
                        if (Search.Count > i + 1)
                        {
                            int[] _result = Search[i].Intersect(Search[i + 1]).ToArray();
                            Search[i + 1].Clear();
                            Search[i + 1].AddRange(_result);
                        }
                    }

                    resultIDs = Search[Search.Count - 1];

                    for (int i = 0; i < resultIDs.Count; i++)
                    {
                        List<string> data = new List<string>();
                        foreach (var t in this.Columns)
                        { 
                            data.Add(t.FindDataByID(resultIDs[i]));
                        }

                        var dl = new T();
                        dl.Init(resultIDs[i], data.ToArray());
                        
                        Boxes.Add(dl);
                    }
                }

                return Boxes.ToArray();
            }
        }


        /// <summary>
        /// Ищет первый id элемента по параметрам, если IsSector = -1, то ищет везде 
        /// </summary>
        /// <param name="ColumnName"></param>
        /// <param name="Data"></param>
        /// <param name="InSector"></param>
        /// <returns></returns>
        public virtual int GetIDByParams(string ColumnName, string Data, int InSector = -1)
        {
            int result = -1;

            if (InSector == -1)
            {
                for (int i = 0; i < Settings.CountClusters; i++)
                {
                    result = GetIDByParams(ColumnName, Data, i);

                    if (result != -1)
                    {
                        break;
                    }
                }

            }
            else
            {
                _LoadDataBase(InSector);

                result = this[ColumnName].FindID(Data);
            }
            return result;
        }

        /// <summary>
        /// Ищет первый id элемента по параметрам, если IsSector = -1, то ищет везде 
        /// </summary>
        /// <param name="ColumnName"></param>
        /// <param name="Data"></param>
        /// <param name="InSector"></param>
        /// <returns></returns>
        public virtual int GetIDByParams(Interfaces.AColumn aColumn, string Data, int InSector = -1)
        {
            return GetIDByParams(aColumn.Name, Data, InSector);
        }

        /// <summary>
        /// Находит строку данных по id
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="InSector"></param>
        /// <returns></returns>
        public virtual string[] GetDataByID(int ID)
        {
            lock (Columns)
            {
                List<string> strings = new List<string>();

                _LoadDataBase((int)GetSectorByID((uint)ID));

                foreach (var t in Columns)
                {
                    strings.Add(t.FindDataByID(ID));
                }
                return strings.ToArray();
            }
        }

        /// <summary>
        /// Возвращет строку по id в виде ItemData[]
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        public virtual ItemData[] GetItemsDataByID(int ID)
        {
            List<ItemData> data = new List<ItemData>();
            foreach (var t in GetDataByID(ID))
            {
                data.Add(new ItemData(ID, t));
            }
            return data.ToArray();
        }

        public virtual T GetDataLineByID<T>(int ID) where T : IDatRows
        {
            var line = Activator.CreateInstance<T>();
            line.Init(ID, GetDataByID(ID));
            return line;
        }

        /// <summary>
        /// Ищет и возвращает первую строку подходящую под введенные параметры возврат через массив ячеек, если не находит => массив пустой
        /// </summary>
        /// <param name="ColumnName"></param>
        /// <param name="Data"></param>
        /// <param name="InSector">Если -1, то ищет во всех сразу, иначе в загруженном</param>
        /// <returns></returns>
        public virtual ItemData[] GetDataInBaseByColumnName(string ColumnName, string Data, int InSector = -1)
        {
            lock (Columns)
            {
                List<ItemData> _data = new List<ItemData>();

                bool Use = false;//маркер о том что поиск в {InSector} секторе уже был 

                for (int i = 0; i < Settings.CountClusters; i++)
                {
                    if (InSector != -1 && Use == true)
                    {
                        if (Use == true)
                        {
                            break;
                        }
                        else
                        {
                            Use = true;
                            _LoadDataBase(InSector);
                        }
                    }
                    else
                    {
                        _LoadDataBase(i);
                    }


                    int id = this[ColumnName].FindID(Data);

                    if (id != -1)
                    {
                        foreach (Interfaces.AColumn table1 in Columns)
                        {
                            _data.Add(new ItemData(id, table1.FindDataByID(id)));
                        }
                        return _data.ToArray();
                    }

                }

                return _data.ToArray();
            }
        }

        public virtual ItemData GetDataByParams(string ColumnName, int ID)
        {
            var Sector = GetSectorByID((uint)ID);
            _LoadDataBase((int)Sector);
            return new ItemData(ID, this[ColumnName].FindDataByID(ID));
        }

        public virtual ItemData GetDataByParams(AColumn Column, int ID)
        {
            return GetDataByParams(Column.Name, ID);
        }
        #endregion

        #region Индексаторы
        public virtual AColumn this[string columnName]
        {
            get
            {
                lock (Columns)
                {
                    foreach (Column Column in Columns)
                    {
                        if (Column.Name == columnName)
                        {
                            return Column;
                        }
                    }

                    throw new IndexOutOfRangeException(ExeptionThereIsNotColumn);
                } 
            }
            protected set
            {
                lock (Columns)
                {
                    for (int i = 0; i < Columns.Count; i++)
                    {
                        if (Columns[i].Name == columnName)
                        {
                            Columns[i] = value; return;
                        }
                    }

                    throw new IndexOutOfRangeException(ExeptionThereIsNotColumn);
                }
            }
        }

        public virtual AColumn this[int index]
        {
            get
            {
                return Columns[index];
            }
            protected set
            {
                lock (Columns)
                {
                    Columns[index] = value;
                }
            }
        }
        #endregion
    
        public void InitManager(DatabaseManager dataBaseManager) { _myManager = _myManager == null ? dataBaseManager : _myManager; }

        public void Dispose()
        {

        }
    }
}
