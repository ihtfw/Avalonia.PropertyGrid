# Avalonia.PropertyGrid
This is a PropertyGrid implementation for Avalonia, you can use it in Avalonia Applications.  
Its main features are:  
* Support automatically analyze object properties and display, like DevExpress 's PropertyGrid of WinForms
* Support simultaneous editing of multiple objects in one PropertyGrid
* Support ICustomTypeDescriptor
* Support BindingList/Array editing, support dynamic addition and deletion. 
* Support data verification.
* Support dynamic visibility
* Support two display modes: category-based and alphabetical sorting  
* Support text filtering, regular expression filtering, and supports ignoring case settings  
* Support fast filtering by Category
* Support data automatic reloading
* Support automatic expansion of sub-objects
* Support adjust the width of the property name and property value by drag the title
* Support localization
* Supprot Custom Cell Edit

## How To Use
Use the source code of this project directly or add packages from nuget(https://www.nuget.org/packages/bodong.Avalonia.PropertyGrid).  
Then add PropertyGrid to your project, and bind the object to be displayed and edited to the SelectObject property. If you want to bind multiple objects, just bind IEnumerable<T> directly

## Detail Description
### Data Modeling
If you want to edit an object in PropertyGrid, you only need to directly set this object to the SelectedObject property of PropertyGrid, PropertyGrid will automatically analyze the properties that can support editing, and edit it with the corresponding CellEdit. At the same time, you can also use Attributes in System.ComponentModel and System.ComponentModel.DataAnnotations to mark these properties, so that these properties have some special characteristics.  
Support but not limited to these:
```
System.ComponentModel.CategoryAttribute
System.ComponentModel.BrowsableAttribute
System.ComponentModel.ReadOnlyAttribute
System.ComponentModel.DisplayNameAttribute
System.ComponentModel.DescriptionAttribute
System.ComponentModel.DataAnnotations.EditableAttribute
```
In addition, there are other classes that can be supported in Avalonia.PropertyGrid.Model.ComponentModel and Avalonia.PropertyGrid.Model.ComponentModel.DataAnnotations, which can assist in describing class properties.  
If you want to have some associations between your class properties, for example, some properties depend on other properties in implementation, then you can try to mark this dependency with Avalonia.PropertyGrid.Model.ComponentModel.DataAnnotations.DependsOnPropertyAttribute  
but you need to inherit your class from Avalonia.PropertyGrid.Model.ComponentModel.ReactiveObject, otherwise you need to maintain this relationship by yourself, just trigger the PropertyChanged event of the target property when the dependent property changes.  
**Struct properties are not supported.**  

### Extra Data Structure
* Avalonia.PropertyGrid.Model.Collections.SelectableList<T>  
    You can initialize this list with some objects, and you can only select one object in this list. ProeprtyGrid uses ComboBox by default to edit the properties of this data structure
* Avalonia.PropertyGrid.Model.Collections.CheckedList<T>
    like SelectableList<T>, you can initialize it with some objects, but you can select multiple objects in it. ProeprtyGrid uses a set of CheckBoxes by default to edit the properties of this data structure, for example:
    ![CheckList](./Docs/Images/CheckList.png)

### Data Reloading
Implement from System.ComponentModel.INotifyPropertyChanged and trigger the PropertyChanged event when the property changes. PropertyGrid will listen to these events and automatically refresh the view data.  
if you implementing from Avalonia.PropertyGrid.Model.ComponentModel.INotifyPropertyChanged instead of System.ComponentModel.INotifyPropertyChanged will gain the additional ability to automatically fire the PropertyChanged event when an edit occurs in the PropertyGrid without having to handle each property itself.

### Change Size
You can change the width of namelabel and cell edit by drag here:
![Dragging](./Docs/Images/ChangeSize.png)

### Multiple Objects Edit

If you want to edit multiple objects at the same time, you only need to set the object to SelectedObject as IEnumerable, for example:

```C#
public IEnumerable<SimpleObject> multiObjects => new SimpleObject[] { multiObject0, multiObject1 };
```
```xml
<pgc:PropertyGrid x:Name="propertyGrid_MultipleObjects" Margin="2" SelectedObject="{Binding multiObjects}"></pgc:PropertyGrid>
```
**Due to complexity considerations, there are many complex types of multi-object editing that are not supported!!!**

### ICustomTypeDescriptor
You can find usage examples directly in Samples

### Array Support
PropertyGrid supports array editing. The array properties here can only be declared using BindingList. Setting [Editable(false)] can disable the creation and deletion functions, which is consistent with the behavior of Array. In addition, in order to support creation functions, **the template parameters of BindingList can only be non-pure virtual classes.**   
**Struct properties are not supported.**

### Expand Class Properties
When PropertyGrid does not provide a built-in CellEdit to edit the target property, there are several possibilities:
1. If the property or the PropertyType of property is marked with TypeConverter, then the PropertyGrid will try to use the TextBox to edit the object. When the text is changed, it will actively try to use TypeConverter to convert the string into the target object.
2. If the property uses ExpandableObjectConverter, then PropertyGrid will try to expand the object in place.
3. If neither is satisfied, then PropertyGrid will try to use a read-only TextBox to display the ToString() value of the target property.

```C#
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class EncryptData : MiniReactiveObject
    {
        public EncryptionPolicy Policy { get; set; } = EncryptionPolicy.RequireEncryption;

        public RSAEncryptionPaddingMode PaddingMode { get; set; } = RSAEncryptionPaddingMode.Pkcs1;
    }

    #region Expandable
    [DisplayName("Expand Object")]
    [Category("Expandable")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public LoginInfo loginInfo { get; set; } = new LoginInfo();
    #endregion
```

### Data Validation
There are two ways to provide data validation capabilities:
1. Throw an exception directly in the setter of the property. But I personally don't recommend this method very much, because if you set this property in the code, it may cause errors by accident. like:

```C#
    string _SourceImagePath;
    [Category("DataValidation")]
    [PathBrowsable(Filters = "Image Files(*.jpg;*.png;*.bmp;*.tag)|*.jpg;*.png;*.bmp;*.tag")]
    public string SourceImagePath
    {
        get => _SourceImagePath;
        set
        {
            if (value.IsNullOrEmpty())
            {
                throw new ArgumentNullException(nameof(SourceImagePath));
            }

            if (!System.IO.Path.GetExtension(value).iEquals(".png"))
            {
                throw new ArgumentException($"{nameof(SourceImagePath)} must be .png file.");
            }

            _SourceImagePath = value;
        }
    }
```
2. The second method is to use System.ComponentModel.DataAnnotations.ValidationAttribute to mark the target property, both system-provided and user-defined. for example:
```C#
    public class ValidatePlatformAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is CheckedList<PlatformID> id)
            {
                if (id.Contains(PlatformID.Unix) || id.Contains(PlatformID.Other))
                {
                    return new ValidationResult("Can't select Unix or Other");
                }
            }

            return ValidationResult.Success;
        }
    }

    [Category("DataValidation")]
    [Description("Select platforms")]
    [ValidatePlatform]
    public CheckedList<PlatformID> Platforms { get; set; } = new CheckedList<PlatformID>(Enum.GetValues(typeof(PlatformID)).Cast<PlatformID>());

    [Category("Numeric")]
    [Range(10, 200)]
    public int iValue { get; set; } = 100;

    [Category("Numeric")]
    [Range(0.1f, 10.0f)]
    public float fValue { get; set; } = 0.5f;
    
    [Category("Numeric")]
    [Range(0.1f, 10.0f)]
    [FloatPrecision(3)]
    public float fValuePrecision { get; set; } = 0.5f;
```

### Dynamic Visibilty
By setting Attribute, you can make certain Properties only displayed when conditions are met. for example: 
```C#
    public class DynamicVisibilityObject : ReactiveObject
    {
        [ConditionTarget]
        public bool IsShowPath { get; set; } = true;

        [VisibilityPropertyCondition(nameof(IsShowPath), true)]
        [PathBrowsable(Filters = "Image Files(*.jpg;*.png;*.bmp;*.tag)|*.jpg;*.png;*.bmp;*.tag")]
        public string Path { get; set; } = "";

        [ConditionTarget]
        public PlatformID Platform { get; set; } = PlatformID.Win32NT;

        [VisibilityPropertyCondition(nameof(Platform), PlatformID.Unix)]
        [ConditionTarget]
        public string UnixVersion { get; set; } = "";

        // show more complex conditions...
        [Browsable(false)]
        [DependsOnProperty(nameof(IsShowPath), nameof(Platform), nameof(UnixVersion))]
        [ConditionTarget]
        public bool IsShowUnixLoginInfo => IsShowPath && Platform == PlatformID.Unix && UnixVersion.IsNotNullOrEmpty();

        [VisibilityPropertyCondition(nameof(IsShowUnixLoginInfo), true)]
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public LoginInfo unixLogInInfo { get; set; } = new LoginInfo();
    }
```
In this example, you can check IsShowPath first, then set the Platform to Unix, and then enter something in UnixVersion, and you will see the unixLoginInfo field.
To do this, you only need to mark the property with a custom Attribute. If you need to implement your own rules, just implement your own rules from **AbstractVisiblityConditionAttribute**.  
One thing to pay special attention to is **that any property that needs to be used as a visibility condition for other properties needs to be marked with [ConditionTarget].**   
The purpose is to let PropertyGrid know that when this property changes, it needs to notify the upper layer to refresh the visibility information.

### User Localization
Implement your Avalonia.PropertyGrid.Model.Services.ILocalizationService class, and register its instance by :
```C#
    PropertyGrid.LocalizationService.AddExtraService(new YourLocalizationService());
```

### Custom Cell Edit
To customize CellEdit, you need to implement a Factory class from AbstractCellEditFactory, and then append this class instance to PropertyGrid.FactoryTemplates, such as:
```C#
    public class ToggleSwitchExtensionPropertyGrid : Controls.PropertyGrid
    {
        static ToggleSwitchExtensionPropertyGrid()
        {
            FactoryTemplates.AddFactory(new ToggleSwitchCellEditFactory());
        }
    }

    class ToggleSwitchCellEditFactory : AbstractCellEditFactory
    {
        // make this extend factor only effect on ToggleSwitchExtensionPropertyGrid
        public override bool Accept(object accessToken)
        {
            return accessToken is ToggleSwitchExtensionPropertyGrid;
        }

        public override Control HandleNewProperty(object target, PropertyDescriptor propertyDescriptor)
        {
            if (propertyDescriptor.PropertyType != typeof(bool))
            {
                return null;
            }

            ToggleSwitch control = new ToggleSwitch();
            control.Checked += (s, e) => { SetAndRaise(control, propertyDescriptor, target, true); };
            control.Unchecked += (s, e) => { SetAndRaise(control, propertyDescriptor, target, false); };

            return control;
        }

        public override bool HandlePropertyChanged(object target, PropertyDescriptor propertyDescriptor, Control control)
        {
            if (propertyDescriptor.PropertyType != typeof(bool))
            {
                return false;
            }

            ValidateProperty(control, propertyDescriptor, target);

            if (control is ToggleSwitch ts)
            {
                ts.IsChecked = (bool)propertyDescriptor.GetValue(target);

                return true;
            }

            return false;
        }
    }
```
There are only two methods that must be overridden:   
HandleNewProperty is used to create the control you want to edit the property, and you need to pass the edited data of the property by yourself.  
HandleProeprtyChanged method is used to synchronize external data. When the external data changes, the data is reacquired and synchronized to the control.
AbstractCellEditFactory also has a overrideable property ImportPriority. This value determines the order in which the PropertyGrid triggers these Factories. The larger the value, the earlier the trigger.   
Overriding the Accept method allows your Factory to only take effect when appropriate.

## Description of Samples
![Basic View](./Docs/Images/BasicView.png)
You can clone this project, and open Avalonia.PropertyGrid.sln, build it and run Avalonia.PropertyGrid.Samples, you can view this.

### Basic
This page shows the basic functions of PropertyGrid, including the display of various properties and the default editor, etc.  

### Views
![Views](./Docs/Images/Views.png)
Test all adjustable appearance properties.

### DataSync
Here you can verify data changes and auto-reload functionality.

### MultiObjects
You can verify the function of multi-object editing here. Note:   
**some properties do not support editing multiple objects at the same time.**

### CustomObject
![CustomObject](./Docs/Images/CustomObject.png)
Here shows how to create a custom object based on ICustomTypeDescriptor.

### Custom Cell Edit
![CustomCellEdit](./Docs/Images/CustomCellEdit.png)
By default, PropertyGrid uses CheckBox to edit Boolean data, here shows how to use ToggleSwitch to edit Boolean data in a simple way, and how to make this function only effective locally without affecting the whole.

### Dynamic Visibility
![DynamicVisibility](./Docs/Images/DynamicVisibility.png)
Show Dynamic Visibility 

## Avalonia.PropertyGrid.NugetSamples
This example shows how to use PropertyGrid through the Nuget package. Its content is similar to the Sample that directly uses the source code, and it can also be used as a case for learning how to use it.  

