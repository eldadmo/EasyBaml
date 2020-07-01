# EasyBaml
Easy language support for xaml
========================================================================
Easy Baml 2017
========================================================================

Implementation of BAML WPF localization approach in a user(WPF developer)-friendly and easy way.
Originally develop as Visual studio Addin and archives by original author skiba_k,
in a few mouse clicks prepare your application to localization and then finally get localized application.

Main features

WPF localization using BAML, similar to using LocBaml
Fully automatic operationing, no command line expirience
Generates minimum UID attributes in XAML with clear name
Only really localizable attributes are extracted to translation files
Localizion files are .resx files (not CSV), which allows translator to use familiar tools
Translation files are updated with new keys, without overwriting translated strings
Build is fully automized, also can be performed on TFS build server
Works smooth with signing assemblies and click-once manifest
Works smooth with Source Control
Localizable string resources (e.g. "Properties.Resources.MyString") as usually may stay in satellite assemblies
Easy and without overhead support any number of languages
Now supports VS 2017 community and professional.
Now supports WPF CS and VB projects (not Silverlight)

---------------------------------------------------------------------
Release 1.0.8 - From original author
---------------------------------------------------------------------
includes improved stability and compilation time.

---------------------------------------------------------------------
Release 1.0.9
---------------------------------------------------------------------
Now supprot Visual Studio 2013 Addin.

---------------------------------------------------------------------
Release 1.1.0
---------------------------------------------------------------------
Addin Convert into Vspackage VSIX file to be installed into vs 2017 and above.

---------------------------------------------------------------------
Release 1.1.1
---------------------------------------------------------------------
Fix major compilation problem from the original 1.0.8 version.
