using System;
using System.Collections.Generic;
using QuickWheel.Interfaces;
using QuickWheel.Models;
using QuickWheel.Services.Actions;

namespace QuickWheel.Services
{
    public class ActionFactory
    {
        private readonly Dictionary<SliceType, ISliceAction> _actions;

        public ActionFactory()
        {
            _actions = new Dictionary<SliceType, ISliceAction>
            {
                { SliceType.App, new AppAction() },
                { SliceType.Web, new WebAction() },
                { SliceType.Paste, new PasteAction() }
            };
        }

        public void Execute(SliceConfig config)
        {
            if (_actions.TryGetValue(config.Type, out var action))
            {
                action.Execute(config);
            }
        }
    }
}
