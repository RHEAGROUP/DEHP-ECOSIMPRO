﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BrowserEcosimProHeader.xaml.cs" company="RHEA System S.A.">
//    Copyright (c) 2015-2020 RHEA System S.A.
//
//    Author: Sam Gerené, Alex Vorobiev, Alexander van Delft, Nathanael Smiechowski.
//
//    This file is part of DEHPEcosimPro
//
//    The DEHPEcosimPro is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 3 of the License, or (at your option) any later version.
//
//    The DEHPEcosimPro is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public License
//    along with this program; if not, write to the Free Software Foundation,
//    Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DEHPEcosimPro.Views
{
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for <see cref="BrowserEcosimProHeader"/> XAML
    /// </summary>
    public partial class BrowserEcosimProHeader : UserControl
    {
        /// <summary>
        /// The dependency property whether the header sould display the option title and its value 
        /// </summary>
        public static readonly DependencyProperty IsOptionDependantProperty = DependencyProperty.Register("IsOptionDependant", typeof(bool), typeof(BrowserEcosimProHeader), new PropertyMetadata(false));
        
        /// <summary>
        /// Initializes a new instance of the <see cref="BrowserEcosimProHeader"/> class.
        /// </summary>
        public BrowserEcosimProHeader()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Gets or sets the <see cref="IsOptionDependant"/> dependency property.
        /// </summary>
        public bool IsOptionDependant
        {
            get => (bool)this.GetValue(IsOptionDependantProperty);
            set => this.SetValue(IsOptionDependantProperty, value);
        }
    }
}