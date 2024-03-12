﻿using NASDatabase.Interfaces;
using NASDatabase.Server.Data;
using System;
using System.Collections.Generic;


namespace NASDatabase.SmartSearchSettings
{
    internal class StopWith : ISearch
    {
        public List<int> SearchID(AColumn ColumnParams, AColumn In, string Params)
        {
            List<int> data = new List<int>();
           
            foreach (var p in In.GetDatas())
            {
                if(p.Data.EndsWith(Params))
                    data.Add(p.ID);
            }

            return data;
        }
    }
}
