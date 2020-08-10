## Wouldn't it be nice
# Applying a custom TinyMCE configuration to an Episerver page property with an attribute

Episerver uses the TinyMCE as the editor for rich text input. Since 2018 the Episerver TinyMCE package has been upgraded to version 2. The current version, 2.10.1 at the time of writing, corresponds with TinyMce 4.8.5. The Episerver TinyMCE package comes with some default configuration, which can be changed through an Episerver initialization module. In the Alloy sample project this initialization module is called ExtendedTinyMceInitialization, and can be found in the Business/Initialization folder.

In this initialization module it is possible to define customized TinyMCE configurations for specific properties on specific page types. For instance, you might want to remove the button to insert an image for some rich text fields, or you might want to add additional formats to the formatting list.

To apply a custom configuration the following syntax is used:

```csharp
config.For<StandardPage>(x => x.MainBody, simpleConfig);
```

where `config` is the TinyMceConfiguration object, and `simpleConfig` is a custom TinyMCE configuration previously defined. 

I have always found it awkward to have to set the custom configuration for properties in the initializaton module. Each page property to which you want to apply the custom configuration needs a separate line in the initialization module. Wouldn't it be nice to be able to apply the configuration in the page model class when definining the page properties, with an attribute, like so:

```csharp
[SimpleTinyMceConfig]
public virtual XhtmlString MainBody { get; set; }
```

The code necessary to bring this about is described in this blog. The code can also be found in an Alloy sample project on GitHub, at:
https://github.com/sentialabs/TinyMceConfigAttributes


In the new setup the only elements in the initialization module are:
1. the definition of the custom TinyMCE configuration, and
2. a call to register the attribute which will be used to apply the custom configuration to a property.

The definition of a custom TinyMCE configuration looks something like this

```csharp
var simpleConfig = config.Default().Clone()
.AddPlugin("wordcount code")
.AppendToolbar("code")
.RemovePlugin("image")
.RemovePlugin("epi-image-editor")
.Height(125);
```

The Episerver documentation on customizing the TinyMCE editor can be found <a href="https://world.episerver.com/documentation/developer-guides/CMS/add-ons/customizing-the-tinymce-editor-v2/" target="_blank">here</a>, 
we won't go into that.

The registration of the attribute is going to look like this, and will be explained below:
```csharp
TinyMceCustomSettingsAttributeRegistration<SimpleTinyMceConfigAttribute>
                            .RegisterCustomTinyMceSettingsAttribute(config, simpleConfig );
```

## Defining an attribute

The attribute doesn't really need any code:

```csharp
public class SimpleTinyMceConfigAttribute : BaseTinyMceCustomSettingsAttribute
{
}
```

To prevent the attribute registration being applied to any class, we let the attribute inherit from `BaseTinyMceCustomSettingsAttribute`.

The `BaseTinyMceCustomSettingsAttribute` holds the indication of the attribute usage, but also has no code.

```csharp
[AttributeUsage(AttributeTargets.Property)]
public abstract class BaseTinyMceCustomSettingsAttribute : Attribute
{
}
``` 

## Registering the attribute

The actual work is done in the attribute registration code. In this case attribute registration actually means: applying the custom TinyMCE configuration to every property decorated with the attribute. 

The registration call looks like this:
```csharp
TinyMceCustomSettingsAttributeRegistration<SimpleTinyMceConfigAttribute>
                .RegisterCustomTinyMceSettingsAttribute(config, simpleConfig );
```

In this call three elements are passed:
1. The attribute type `<SimpleTinyMceConfigAttribute>`.
2. The TinyMceConfiguration object, which holds all configurations.
3. The custom TinyMCE setting object, which will be applied to all properties decorated with the passed attribute type.


The basic steps which we need to go through are:
1. Get a list of all Episerver page and block types
2. Loop through all these content types
3. Find the properties decorated with the attribute for each content type
4. Call a generic version of the `For` extension method for each decorated property.

The original version of the extension method looks like this:

```csharp
config.For<StandardPage>(x => x.MainBody, simpleConfig);
```

The first problem is that when looping through the Episerver page and block types, we need to pass the content type as parameter to extension method above. Where it says `<StandardPage>` a type has to be inserted as a parameter. We need some reflection.

```csharp
 var theForMethod = typeof(TinyMceConfiguration).GetMethod("For");
 var theGenericMethod = theForMethod.MakeGenericMethod(contentType);
```

In the code above `contentType` is a parameter of type `Type`, corresponding to an Episerver page or block type.

The second problem is getting the parameters with which to invoke the generic method on a property, for which we also need reflection.
We have the properties as a list of `PropertyInfo` objects. We need to build a lambda expression with which to apply the custom settings object to the property.

```csharp
var parameter = Expression.Parameter(contentType, "entity");
var property = Expression.Property(parameter, propertyInfo);
var funcType = typeof(Func<,>).MakeGenericType(contentType, typeof(object));
var lambda = Expression.Lambda(funcType, property, parameter);

var parameters = new object[] { lambda, customTinyMceSettings };
```

We can then invoke the method with the parameters.

```csharp
theGenericMethod.Invoke(config, parameters);
```

The signature of the class in which all this happens is this:

```csharp
public static class TinyMceCustomSettingsAttributeRegistration<T> 
                    where T : BaseTinyMceCustomSettingsAttribute
```

By requiring `T` to be of type `BaseTinyMceCustomSettingsAttribute` we prevent the attribute registration method to be called with types which are not meant to be attributes for TinyMCE custom settings.

The whole class looks like this:


```csharp
public static class TinyMceCustomSettingsAttributeRegistration<T> 
                    where T : BaseTinyMceCustomSettingsAttribute
{
    // ReSharper disable once StaticMemberInGenericType
    private static readonly IEnumerable<Type> _listOfEpiserverContentDataTypes;

    static TinyMceCustomSettingsAttributeRegistration()
    {
        _listOfEpiserverContentDataTypes = GetListOfEpiserverContentTypes();
    }

    /// <summary>
    /// Sets custom TinyMceSettings on all XhtmlString properties
    /// decorated with a specific attribute.
    /// </summary>
    /// <param name="config"></param>
    /// <param name="customTinyMceSettings">The custom TinyMceSettings</param>
    public static void RegisterCustomTinyMceSettingsAttribute(TinyMceConfiguration config,
        TinyMceSettings customTinyMceSettings)
    {
        // Get MethodInfo for the extension method usually used to designate
        // custom TinyMceSettings for an XhtmlProperty, viz.
        // config.For<StandardPage>(x => x.MainBody, customTinyMceSettings);
        var theForMethod = typeof(TinyMceConfiguration).GetMethod("For");

        if (theForMethod == null) return;

        foreach (var contentType in _listOfEpiserverContentDataTypes)
        {
            // Get the properties decorated with the attribute 
            // used for designating the custom TinyMceSettings.
            var properties = contentType
                .GetProperties().Where(x => x.CustomAttributes
                                .Any(att => att.AttributeType == typeof(T))).ToList();

            if (!properties.Any()) continue;

            // Make the For method generic.
            var theGenericMethod = theForMethod.MakeGenericMethod(contentType);

            foreach (var propertyInfo in properties)
            {
                // Continue if the attribute is inadvertently applied to a
                // property which is not an XhtmlString.
                if (propertyInfo.PropertyType.Name != "XhtmlString") continue;
                
                var parameter = Expression.Parameter(contentType, "entity");
                var property = Expression.Property(parameter, propertyInfo);
                var funcType = typeof(Func<,>).MakeGenericType(contentType, typeof(object));
                var lambda = Expression.Lambda(funcType, property, parameter);

                var parameters = new object[] { lambda, customTinyMceSettings };

                try
                {
                    theGenericMethod.Invoke(config, parameters);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }

    /// <summary>
    /// Get a list of all types which may
    /// have an XhtmlString property (Pages and Blocks).
    /// </summary>
    /// <returns>A list of types</returns>
    private static IEnumerable<Type> GetListOfEpiserverContentTypes()
    {
        var listOfTypes = (
            from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
            from assemblyType in domainAssembly.GetTypes()
            where typeof(ContentData).IsAssignableFrom(assemblyType)
            select assemblyType).ToArray();

        return listOfTypes;
    }
}

````





