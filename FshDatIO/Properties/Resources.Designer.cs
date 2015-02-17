﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34209
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace FshDatIO.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("FshDatIO.Properties.Resources", typeof(Resources).Assembly);
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
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The header identifier is invalid..
        /// </summary>
        internal static string DatHeaderInvalidIdentifer {
            get {
                return ResourceManager.GetString("DatHeaderInvalidIdentifer", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The width and height of DXT1 images must be a multiple of four..
        /// </summary>
        internal static string DXT1InvalidSize {
            get {
                return ResourceManager.GetString("DXT1InvalidSize", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The width and height of DXT3 images must be a multiple of four..
        /// </summary>
        internal static string DXT3InvalidSize {
            get {
                return ResourceManager.GetString("DXT3InvalidSize", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The image has not been loaded..
        /// </summary>
        internal static string ImageNotLoaded {
            get {
                return ResourceManager.GetString("ImageNotLoaded", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The file is truncated and invalid..
        /// </summary>
        internal static string InvalidFshFile {
            get {
                return ResourceManager.GetString("InvalidFshFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An invalid header was read..
        /// </summary>
        internal static string InvalidFshHeader {
            get {
                return ResourceManager.GetString("InvalidFshHeader", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The size of the DBPF index table is invalid..
        /// </summary>
        internal static string InvalidIndexTableSize {
            get {
                return ResourceManager.GetString("InvalidIndexTableSize", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to This method can only be called when a dat has been loaded from a file..
        /// </summary>
        internal static string NoDatLoaded {
            get {
                return ResourceManager.GetString("NoDatLoaded", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The file at the specified index does not exist..
        /// </summary>
        internal static string SpecifiedIndexDoesNotExist {
            get {
                return ResourceManager.GetString("SpecifiedIndexDoesNotExist", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to find the Fsh file at index number {0}..
        /// </summary>
        internal static string UnableToFindTheFshFileAtIndexNumber_Format {
            get {
                return ResourceManager.GetString("UnableToFindTheFshFileAtIndexNumber_Format", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unsupported compression format..
        /// </summary>
        internal static string UnsupportedCompressionFormat {
            get {
                return ResourceManager.GetString("UnsupportedCompressionFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to DBPF format version {0}.{1} is not supported..
        /// </summary>
        internal static string UnsupportedDBPFVersion {
            get {
                return ResourceManager.GetString("UnsupportedDBPFVersion", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Only 24-bit , 32-bit, DXT1 and DXT3 images are supported..
        /// </summary>
        internal static string UnsupportedFshType {
            get {
                return ResourceManager.GetString("UnsupportedFshType", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to DBPF index version {0} is not supported..
        /// </summary>
        internal static string UnsupportedIndexVersion {
            get {
                return ResourceManager.GetString("UnsupportedIndexVersion", resourceCulture);
            }
        }
    }
}
