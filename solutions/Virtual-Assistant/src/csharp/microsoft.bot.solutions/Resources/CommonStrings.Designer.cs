﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Microsoft.Bot.Solutions.Resources {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "15.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class CommonStrings {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal CommonStrings() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Microsoft.Bot.Solutions.Resources.CommonStrings", typeof(CommonStrings).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to and.
        /// </summary>
        public static string And {
            get {
                return ResourceManager.GetString("And", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to or.
        /// </summary>
        public static string Or {
            get {
                return ResourceManager.GetString("Or", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to dddd, MMMM dd.
        /// </summary>
        public static string SpokenDateFormat {
            get {
                return ResourceManager.GetString("SpokenDateFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to .
        /// </summary>
        public static string SpokenDatePrefix {
            get {
                return ResourceManager.GetString("SpokenDatePrefix", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to at.
        /// </summary>
        public static string SpokenTimePrefix_MoreThanOne {
            get {
                return ResourceManager.GetString("SpokenTimePrefix_MoreThanOne", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to at.
        /// </summary>
        public static string SpokenTimePrefix_One {
            get {
                return ResourceManager.GetString("SpokenTimePrefix_One", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to today.
        /// </summary>
        public static string Today {
            get {
                return ResourceManager.GetString("Today", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to tomorrow.
        /// </summary>
        public static string Tomorrow {
            get {
                return ResourceManager.GetString("Tomorrow", resourceCulture);
            }
        }
    }
}
