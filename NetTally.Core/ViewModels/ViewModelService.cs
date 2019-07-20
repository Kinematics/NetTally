using System;
using System.Net.Http;
using NetTally.Collections;
using NetTally.Output;
using NetTally.VoteCounting;
using NetTally.Web;
using System.Globalization;
using NetTally.Utility.Comparers;

namespace NetTally.ViewModels
{
    public class ViewModelService
    {
        public static ViewModel MainViewModel;

        public ViewModelService(ViewModel model)
        {
            MainViewModel = model;
        }
    }
}
