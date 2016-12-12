﻿using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace weekcs.Languages
{
    // FM.2016.12.12 - released to the public. https://github.com/matrello/WeekCalendar

    public class LocalizedStrings
    {
        private static readonly Strings _strings = new Strings();
        public Strings Strings { get { return _strings; } } 
    }
}
