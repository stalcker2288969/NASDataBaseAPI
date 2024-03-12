﻿using NASDatabase.Client;
using NASDatabase.Interfaces;
using System;


namespace NASDatabase.Server.Handlers.Unsafe.CommandsForDataBase
{
    public class RenameColumn : CommandHandler
    {
        private Action<string, string> _handler;

        private string _name;
        private string _newName;

        public RenameColumn(Action<string, string> Handler)
        {
            _handler = Handler;
        }

        public override void SetData(string data)
        {
            var d = data.Split(BaseCommands.SEPARATION.ToCharArray());
            _name = d[0];
            _newName = d[1];

        }

        public override string Use()
        {
            _handler?.Invoke(_name, _newName);
            return BaseCommands.DONE;
        }
    }
}
