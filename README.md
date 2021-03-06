# CrossCutterN: A Light Weight AOP Tool for .NET
[![Build status](https://ci.appveyor.com/api/projects/status/f2up0iv7yadh5tj7/branch/master?svg=true)](https://ci.appveyor.com/project/keeper013/crosscuttern/branch/master)

## Introduction

**_CrossCutterN_** is a free and lightweight AOP tool for .NET using IL weaving technology. As other AOP tools like postsharp, It helps developers to inject AOP code into their programs.

The advantages of **_CrossCutterN_** comparing with other AOP technologies include:

* **Free**: **_CrossCutterN_** is open source and free under MIT license.
* **Light Weight**: Instead of adding compile time dependency to projects, **_CrossCutterN_** injects AOP code after project assemblies are built. This approach allows AOP code injection into assemblies whose source code are not available, and decouples project code from AOP code as much as possible.
* **Out of the box aspect switching support**: **_CrossCutterN_** allows users to switch on/off AOP code that is injected to methods/properties during project run-time at multiple granularity levels.
* **Designed for optimized performance**: **_CrossCutterN_** uses IL weaving technology to make the injected AOP code work as efficient as directly coded in target projects, and the implementation is optimized to avoid unnecessary local variable initializations and method calls.

## Quick Examples:

To perform weave AOP code into an assembly, **_CrossCutterN_** requires the following process:

* Prepare the AOP code module following **_CrossCutterN_** convention. The AOP code content is fully customizable by developers.
* Prepare the configuration file for the AOP module. The configuration file format is quite simple which will be explained in following sections.
* Prepare the configuration file for the target module, which requires the AOP code to be injected to. The configuration file format is quite simple which will be explained in following sections.
* Execute console application tool to weave the original assembly together with the AOP code information into a new assembly.

And it's done.

Let's take a very simple C# method for example:

```C#
namespace CrossCutterN.Sample.Target
{
    using System;

    internal class Target
    {
        public static int Add(int x, int y)
        {
            Console.Out.WriteLine("Add starting");
            var z = x + y;
            Console.Out.WriteLine("Add ending");
            return z;
        }
    }
}
```

When executed, the output to console would be:

```
Add starting
Add ending
```

What if I want to inject some AOP code to the Add method? For example, log the function call and all it's parameter values upon entering the method call, and log the return value before the method returns?

Before going through with the examples, please build of CrossCutterN.Console project, and copy the output binaries to CrossCutterN.Sample\CrossCutterN.Console\ folder. Besides, before executing console application tool, please make sure the sample target project is rebuilt to have a refreshed target assembly to perform IL weaving to.

### Using Name of Methods to Find Target Methods to Be Injected

By following the steps listed:

#### Implement AOP Module

Implement some utility properties and methods first:

```C#
namespace CrossCutterN.Sample.Advice
{
    using System;
    using System.Text;
    using CrossCutterN.Base.Metadata;

    internal sealed class Utility
    {
        internal static string CurrentTime => DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff tt");

        internal static string GetMethodInfo(IExecution execution)
        {
            var strb = new StringBuilder(execution.Name);
            strb.Append("(");
            if (execution.Parameters.Count > 0)
            {
                foreach (var parameter in execution.Parameters)
                {
                    strb.Append(parameter.Name).Append("=").Append(parameter.Value).Append(",");
                }

                strb.Remove(strb.Length - 1, 1);
            }

            strb.Append(")");
            return strb.ToString();
        }

        internal static string GetReturnInfo(IReturn rReturn) 
            => rReturn.HasReturn ? $"returns {rReturn.Value}" : "no return";
    }
}
```
Please note that IExecution and IReturn interfaces are provided by **CrossCutterN.Base.dll** assembly. For **_CrossCutterN_** tool to work, developers must follow it's conventions and provided interfaces.

Now implement methods to output logs upon entry and before return of a method:

```C#
namespace CrossCutterN.Sample.Advice
{
    using System;
    using CrossCutterN.Base.Metadata;

    public static class AdviceByNameExpression
    {
        public static void OnEntry(IExecution execution)
            => Console.Out.WriteLine($"{Utility.CurrentTime} Injected by method name on entry: {Utility.GetMethodInfo(execution)}");

        public static void OnExit(IReturn rReturn)
            => Console.Out.WriteLine($"{Utility.CurrentTime} Injected by method name on exit: {Utility.GetReturnInfo(rReturn)}");
    }
}
```

Just for easy demonstration purpose we directly output the log to console. AOP module implementation is done.

#### Prepare AOP Module Configuration

Add a json file to the AOP module project, make sure it's copied together with the assembly. Name the json file as "adviceByNameExpression.json".

```json
{
  "CrossCutterN": {
    "sample": {
      "AssemblyPath": "CrossCutterN.Sample.Advice.dll",
      "Advices": {
        "CrossCutterN.Sample.Advice.AdviceByNameExpression": {
          "testEntry": {
            "MethodName": "OnEntry",
            "Parameters": [ "Execution" ]
          },
          "testExit": {
            "MethodName": "OnExit",
            "Parameters": [ "Return" ]
          }
        }
      }
    }
  }
}
```

Meaning of the configuration file is like the following:

* I have an assembly which contains AOP code to be injected, the key used to refer to this assembly is "**sample**".
* Path of this assembly is "**CrossCutterN.Sample.Advice.dll**"; it's not an absolute path, so the assembly path is relevant to the path of configuration file, in this case it's in the same folder with the configuration file.
* It has the following AOP methods (Namely "**Advices**") to be injected in class "**CrossCutterN.Sample.Advice.AdviceByNameExpression**".
* One method named "**OnEntry**", with one parameter type marked as "**Execution**" (which is the "IExecution" type in C# code). This method will be referred to as "**testEntry**" in target assembly configuration.
* One method named "**OnExit**", with one parameter type marked as "**Return**" (which is the "IReturn" type in C# code). This method will be referred to as "**testExit**" in target assembly configuration.

#### Prepare Target Module Configuration

Add a json file to the target module project, and make sure it's copied together with the assembly to be injected with AOP method call. Name the json file as "nameExpressionTarget.json".

```json
{
  "CrossCutterN": {
    "DefaultAdviceAssemblyKey": "sample",
    "AspectBuilders": {
      "aspectByMethodName": {
        "AspectBuilderKey": "CrossCutterN.Aspect.Builder.NameExpressionAspectBuilder",
        "Includes": [ "CrossCutterN.Sample.Target.Target.Ad*" ],
        "Advices": {
          "Entry": { "MethodKey": "testEntry" },
          "Exit": { "MethodKey": "testExit" }
        }
      }
    },
    "Targets": {
      "CrossCutterN.Sample.Target.exe": { "Output": "CrossCutterN.Sample.Target.exe" }
    }
  }
}

```

Meaning of the configuration file is like the following:

* I have a default AOP code module which can be referred to as "**sample**"
* The following **AspectBuilders** are defined to help me to do the injection.
* One aspect builder can be referred to as "**CrossCutterN.Aspect.Builder.NameExpressionAspectBuilder**". This reference is implemented and provided by **_CrossCutterN_** tool which will find methods to inject AOP code into by checking the methods' names.
* This aspect builder will inject all methods whose full name is like "**CrossCutterN.Sample.Target.Target.Ad\***"
* This aspect builder will inject a method call to a method which can be referred to as "**testEntry**" upon "**Entry**" of the target method call.
* This aspect builder will inject a method call to a method which can be referred to as "**testExit**" before "**Exit**" of the target method call.
* AOP code added by this aspect builder can be referred to as "**aspectByMethodName**" in configuration for ordering and C# code to switch on/off.
* One assembly is in the **Targets** assemblies to be injected. The assembly is "**CrossCutterN.Sample.Target.exe**". It's not an absolute path, so the path is relevant to the configuration file, in this case it's in the same folder of the configuration file. The weaved assembly will be saved as "CrossCutterN.Sample.Target.exe", path relevant to the configuration file, in this case also the same folder of the configuration file. The file name of the output assembly is exactly the same with the target assembly, so the original assembly will be overwritten by the weaved one.

#### Execute Console Application Tool

Build the AOP and target assemblies with Release configuration, navigate to CrossCutterN.Sample\ folder, execute:
```batch
CrossCutterN.Console\CrossCutterN.Console.exe /d:CrossCutterN.Sample.Advice\bin\Release\adviceByNameExpression.json /t:CrossCutterN.Sample.Target\bin\Release\nameExpressionTarget.json
```

Meaning of the command is:

Execute console application of **_CrossCutterN_**, using **CrossCutterN.Sample.Advice\bin\Release\adviceByNameExpression.json** file as AOP code assembly configuration, and using **CrossCutterN.Sample.Target\bin\Release\nameExpressionTarget.json** file as target assembly configuration.

If the execution is successful, the original CrossCutterN.Sample.Target.exe file is replaced with newly generated one. Execute the new assembly, something like the following output is expected:

```
yyyy-MM-dd HH:mm:ss fff tt Injected by method name on entry: Add(x=1,y=2)
Add starting
Add ending
yyyy-MM-dd HH:mm:ss fff tt Injected by method name on exit: returns 3
```

The result suggests that the AOP method calls have been successfully injected.

To keep the original target assembly for comparation or other purposes, just change the "**Output**" configuration in "**Targets**" section in target assembly configuration to other values than the assembly name of the original, in this case maybe "CrossCutterN.Sample.Target.Weaved.exe" or something else.

### Using Custom Attributes to Mark Target Methods to Be Injected

**_CrossCutterN_** tool also provides a way to mark target methods to be injected using customzed attributes. And the process is similar to the previous:

#### Implement AOP Module

```C#
namespace CrossCutterN.Sample.Advice
{
    using System;
    using CrossCutterN.Base.Concern;
    using CrossCutterN.Base.Metadata;

    public static class AdviceByAttribute
    {
        public static void OnEntry(IExecution execution) 
            => Console.Out.WriteLine($"{Utility.CurrentTime} Injected by attribute on entry: {Utility.GetMethodInfo(execution)}");

        public static void OnExit(IReturn rReturn) 
            => Console.Out.WriteLine($"{Utility.CurrentTime} Injected by attribute on exit: {Utility.GetReturnInfo(rReturn)}");
    }

    public sealed class SampleConcernMethodAttribute : ConcernMethodAttribute
    {
    }
}
```

Note that this time there is an attribute "**SampleConcernMethodAttribute**" declared for marking target methods. This attribute should be added to the target "**Add**" method:

```C#
namespace CrossCutterN.Sample.Target
{
    using System;

    internal class Target
    {
        [CrossCutterN.Sample.Advice.SampleConcernMethod]
        public static int Add(int x, int y)
        {
            Console.Out.WriteLine("Add starting");
            var z = x + y;
            Console.Out.WriteLine("Add ending");
            return z;
        }
    }
}
```

#### Prepare AOP Module Configuration

```json
{
  "CrossCutterN": {
    "sample": {
      "AssemblyPath": "CrossCutterN.Sample.Advice.dll",
      "Attributes": { "method": "CrossCutterN.Sample.Advice.SampleConcernMethodAttribute" },
      "Advices": {
        "CrossCutterN.Sample.Advice.AdviceByAttribute": {
          "entry1": {
            "MethodName": "OnEntry",
            "Parameters": [ "Execution" ]
          },
          "exit1": {
            "MethodName": "OnExit",
            "Parameters": [ "Return" ]
          }
        }
      }
    }
  }
}
```

In **Attributes** section an attribute of type "**CrossCutterN.Sample.Advice.SampleConcernMethodAttribute**" is defined to mark target methods. It can be referred to as "**method**" in target configurations. The configuration file name is adviceByAttribute.json.

#### Prepare Target Module Configuration

```json
{
  "CrossCutterN": {
    "DefaultAdviceAssemblyKey": "sample",
    "AspectBuilders": {
      "aspectByAttribute": {
        "AspectBuilderKey": "CrossCutterN.Aspect.Builder.ConcernAttributeAspectBuilder",
        "ConcernMethodAttributeType": { "TypeKey": "method" },
        "Advices": {
          "Entry": { "MethodKey": "entry1" },
          "Exit": { "MethodKey": "exit1" }
        }
        //,"IsSwitchedOn": false
      }
    },
    "Targets": {
      "CrossCutterN.Sample.Target.exe": { "Output": "CrossCutterN.Sample.Target.exe" }
    }
  }
}
```

Here **AspectBuilderKey** is changed to "**CrossCutterN.Aspect.Builder.ConcernAttributeAspectBuilder**", which is also implemented and provided by **_CrossCutterN_** tool, it will find methods marked by checking predefined attributes. The configuration file is attributeTarget.json.

#### Execute Console Application Tool

Build the AOP and target assemblies with Release configuration, navigate to CrossCutterN.Sample\ folder, execute:

```batch
CrossCutterN.Console\CrossCutterN.Console.exe /d:CrossCutterN.Sample.Advice\bin\Release\adviceByAttribute.json /t:CrossCutterN.Sample.Target\bin\Release\attributeTarget.json
```

The expected result is similar with previous example when executing the weaved assembly:

```
yyyy-MM-dd HH:mm:ss fff tt Injected by attribute name on entry: Add(x=1,y=2)
Add starting
Add ending
yyyy-MM-dd HH:mm:ss fff tt Injected by attribute name on exit: returns 3
```

### Perform AOP Code Injection Using Multiple Aspect Builders

Surely to inject multiple AOP method calls, multiple aspect builders can be declared in single AOP assemlby configuration files and single target assembly configuration files. Please check the "advice.json" and "target.json" configuration files in the sample project. Detailed processes and results are ignored to reduce text redundancy.

One thing to mentioned though, for multiple aspect builders to work together, AOP method call order must be specified, like the "**Order**" section in "target.json":

```json
"Order": {
  "Entry": [
    "aspectByAttribute",
    "aspectByMethodName"
  ],
  "Exit": [
    "aspectByMethodName",
    "aspectByAttribute"
  ]
}
```

It means when applying multiple aspect builders to one target method, upon entry, method call injected by aspect builder referred to as "**aspectByAttribute**" is applied first, and method call injected by aspect builder referred to as **aspectByMethodName** will be applied after the former. And before exiting the target method call, the injected AOP method call ordering is reversed according to the configuration. Please note that "**Order**" section can be ignored for single aspect builder in target configuration files, but is mandatory for multiple aspect builders in target configuration files.

### Runtime AOP Methods Calls Switching

In case sometimes users intend to temporarily disable some of the AOP methods calls and enable them on later, **_CrossCutterN_** provides a way to switch on and off injected AOP methods calls during program run time.

Note the "//,"IsSwitchedOn": false" configuration item in the samples, it is the configuration entry for such switching:

* If not specified, the AOP method calls injected by the aspect builder will not be switchable, which means they always get executed when the target methods are triggered.
* If set to false, the AOP method calss injected by the aspect builder will be switchable, but by default not executed, unless switched on at runtime. They can be switched on and off during the program run time.
* If set to true, the AOP method calls injected by the aspect builder will be switchable, and by default executed, unless switche off at run time. They can be switched off and on during the program run time.

So we uncommand this configuration entry, save the configuration file, and go through the "Using Custom Attributes to Mark Target Methods to Be Injected" example again, the output of the weaved assembly will not include the AOP output:

```
Add starting
Add ending
```

In the program, execute the following statement before calling the Add method:

```C#
CrossCutterN.Base.Switch.SwitchFacade.Controller.SwitchOn("aspectByAttribute");
```

Note that "**aspectByAttribute**" is the key we used to refer to the aspect builder in target configuration. Go through with the "Using Custom Attributes to Mark Target Methods to Be Injected" example again, the output of the weaved assembly will include the AOP output again.

## Configuration Details

To maximize re-usage of AOP code assemblies, **_CrossCutterN_** defines 3 categories of assemblies:

* **Aspect Definition Assemblies**: Assemblies that define aspects and their corresponding builders.
* **Advice Definition Assemblies**: Assemblies that define attributes used to mark target methods/properties and advice methods (A.K.A. AOP code) to be injected into target methods/properties.
* **Target Assemblies**: Project assemblies that AOP codes are injected into.

For the above assembly categories, each has a dedicated configuration file format.

### Aspect Definition Assembly Configuration

The following is a complete sample of aspect definition assembly configuration:
```json
{
  "CrossCutterN": {
    "<some assembly key>": {
      "AssemblyPath": "<relevant or absolute path to the assembly>",
      "AspectBuilders": {
        "builderkey1": "<builder class 1 full name>",
        "builderkey2": "<builder class 2 full name>",
	...
      }
    }
  }
}
```
* Root element name must be "CrossCutterN".
* Under the root element, it's a dictionary structure, the <some assembly key> is used to refer to the assembly that contains the aspect builder definitions in target configuration. the value contains the aspect builders contained in the assembly. It will be OK to configure multiple aspect assemblies in the dictionary structore, as long as the format is followed.
* "AssemblyPath" element is the value of the relevant path of the assembly file to this configuration file, or else the absolute path of the assembly file.
* "AspectBuilders" element contains a dictionary structure, the key is a string used to refer to the aspect builder, the value is the full class name of the aspect builder.

Aspect builders are supposed to build different aspects according to different configurations. In **_CrossCutterN_**, aspect builder mainly has 2 jobs: 1. To determine if a method/property should be injected with the advice methods contained in the aspect; 2. To provide the set of aspect methods references.

Currently **_CrossCutterN_** provides 2 types of aspect builders. **ConcernAttributeAspectBuilder** identifies methods/properties to be injected by checking the custom attributes as introduces in previous sections, while **NameExpressionAspectBuilder** identifies methods/properties to be injected by checking the full name of methods/properties against configured patterns.

By default **ConcernAttributeAspectBuilder** and **NameExpressionAspectBuilder** are initialized for weaving process without the need for configuration. So if developer only needs these 2 types of aspect builders, they **don't** need to include configuration files for aspect builders. Please not that for **ConcernAttributeAspectBuilder**, the builder key is "CrossCutterN.Aspect.Builder.ConcernAttributeAspectBuilder", and for **NameExpressionAspectBuilder**, the builder key is "CrossCutterN.Aspect.Builder.NameExpressionAspectBuilder". These keys are not supposed to be changed or used for custom aspect builders.

**_CrossCutterN_** provides the extention point for developers to implement their own ways to identify which methods /properties are to be injected with AOP code. If developers intend to implement their own aspects, please follow the instructions below:

* Please implement one aspect that implements the interface _CrossCutterN.Aspect.Aspect.IAspect_.
* Please implement one aspect builder that implements the interface _CrossCutterN.Aspect.Builder.IAspectBuilder_, which will return an instance of the implemented _CrossCutterN.Aspect.Aspect.IAspect_ with its _Build_ method mentioned above. For convenience, directly inheriting from _CrossCutterN.Aspect.Aspect.CrossCutterN.Builder.AspectBuilder_ is encouraged.
* **_CrossCutterN_** will try to call the parameter-less constructor of the implemented aspect builder and bind aspect builder configuration to it, so please make sure that the implemented aspect builder has a public parameter-less constructor, and make all customized configuration items bindable in json format.
* Provide a configuration file to the newly implemented aspect assembly, the content may be like the following:

When starting the weaving process, please pass this configuration file to the weaver tool, and make sure that the content in target configuration is correct to refer to the aspect builders configured in this file. Please refer to target configuration in later sections.

### Advice Definition Assembly Configuration

The following is a complete sample of advice definition assembly configuration:

```json
{
  "CrossCutterN": {
    "sample": {
      "AssemblyPath": "CrossCutterN.Sample.Advice.dll",
      "Attributes": { "method": "CrossCutterN.Sample.Advice.SampleConcernMethodAttribute" },
      "Advices": {
        "CrossCutterN.Sample.Advice.Advices": {
          "attributeEntry": {
            "MethodName": "InjectByAttributeOnEntry",
            "Parameters": [ "Execution" ]
          },
          "attributeExit": {
            "MethodName": "InjectByAttributeOnExit",
            "Parameters": [ "Return" ]
          },
          "expressionEntry": {
            "MethodName": "InjectByMethodNameOnEntry",
            "Parameters": [ "Execution" ]
          },
          "expressionExit": {
            "MethodName": "InjectByMethodNameOnExit",
            "Parameters": [ "Return" ]
          }
        }
      }
    }
  }
}
```
Root element must be "CrossCutterN".
Root element contains a dictionary structure, in this case "sample" is the key which is used to refer to this assembly in target configuration files, and the value element contains definition of attributes and advice methods. It will be OK to configure multiple assemblies in this dictionary structure, as long as the format is followed.
"AssemblyPath" element is used to define relevant or absolute path of the assembly.
"Attributes" element contains a dictionary structure with string keys used to refer to attributes defined, full class name of attributes to be values.
"Advices" element contains a dictionary structure, with class names to be keys, and a dictionary of advice method information inside the class to be values. For each advice method, a string is used as key to refer to the advice method, and it value includes the method name and parameter type list.

#### ConcernAttributeAspectBuilder and Attributes

**_CrossCutterN_** provides one aspect builder **ConcernAttributeAspectBuilder** that allows developers to identify methods/properties to be injected based on attributes they have. Generally it works like "If the method/property has this customized attribute, then this advices of this aspect should be applied to or injected into this method/property." For flexibility, 4 types of attributes are provided for this purpose:

**CrossCutterN.Base.Concern.ConcernClassAttribute**: this attribute marks all methods and properties defined in it to be subjects of AOP code injection, based on it's settings and other overwriting attributes:

It has the following properties:

| Property | Description | Default Value |
| --- | --- | --- |
| ConcernConstructor | if set to false all constructors in the class won't be injected | false |
| ConcernPublic | if set to false, all public methods/properties in the class won't be injected | true |
| ConcernProtected | if set to false, all protected methods/properties in the class won't be injected | false |
| ConcernInternal | if set to false, all internal methods/properties in the class won't be injected | false |
| ConcernPrivate | if set to false, all private methods/properties in the class won't be injected | false |
| ConcernInstance | if set to false, all instance methods/properties in the class won't be injected | true |
| ConcernStatic | if set to false, all static methods/properties in the class won't be injected | true |
| ConcernMethod | if set to false, all static methods in the class won't be injected | true |
| ConcernPropertyGetter | if set to false, all property getters in the class won't be injected | false |
| ConcernPropertySetter | if set to false, all property setters in the class won't be injected | false |

**CrossCutterN.Base.Concern.ConcernMethodAttribute**: this attribute marks methods/property getters/property setters to be subjects of AOP code injection.

**CrossCutterN.Base.Concern.ConcernPropertyAttribute**: this attribute marks properties to be subjects of AOP code injection.

It has the following properties:

| Property | Description | Default Value |
| --- | --- | --- |
| ConcernPropertyGetter | if set to false, getter of the property won't be injected | false |
| ConcernPropertySetter | if set to false, setters of the property won't be injected | false |

**CrossCutterN.Base.Concern.NoConcernAttribute**: this attribute marks methods/properties/property getters/property setters not a subject of AOP code injection.

:exclamation: Please note that for the above attributes, attributes on children will overwrite attributes on parent. For example in concern class attribute ConcernProtected is set to false, but one of its protected method has concern method attribute, that method is still a subject to be injected. Another example, if a property has concern property attribute, but it's getter method has no concern attribute, then the getter method won't be a subject to be injected.

:exclamation: The pre-defined attributes are all declared as abstract to force developers to inherite from them and use their own custom attributes for AOP code injection. The reason for this is to avoid re-using of the same existing attribute for multiple concern injections. It is encouraged to have dedicated set of attributes for different concern's injection.

:exclamation: For the sake of keeping pre-defined attribute properties for injection logic, custom implementation of these 4 attributes must inherit from the above properties. This is enforced by validation of parent class type during weaving process.

:exclamation: Developers are not requested to inherit from all 4 attributes above to build a complete set of attributes. For example, if only few methods needs to be injected, only inheriting **CrossCutterN.Base.Concern.MethodMethodAttribute** is good enough. However, at least on of **CrossCutterN.Base.Concern.ConcernClassAttribute**, **CrossCutterN.Base.Concern.ConcernMethodAttribute** and **CrossCutterN.Base.Concern.ConcernPropertyAttribute** should be inherited to pass the validation of **ConcernAttributeAspectBuilder**.

#### Advice Methods, Join Points and Parameters

Advice methods are methods that will be injected into target projects at join points entry, exception or exit. **_CrossCutterN_** requires advice methods to be static, returns void, and only contains certain types of parameters in a specified order.

Available join points include:

| Join Point | Description |
| --- | --- |
| Entry | upon entry of injected method |
| Exception | upon uncaught exception happened in the injected method |
| Exit | before leaving the injected method |

The allowed parameter type of advice methods include:

| Parameter Type | Configuration String | Description |
| --- | --- | --- |
| CrossCutterN.Base.Metadata.IExecutionContext | Context | This parameter is used for passing objects among the advice methods injected in one method |
| CrossCutterN.Base.Metadata.IExecution | Execution | This parameter contains execution method information, including method information, name, value and custom attributes of each input parameter |
| System.Exception | Exception | This is the exception thrown, only available for exception join point |
| CrossCutterN.Base.Metadata.IReturn | Return | This parameter contains the return type and value of the method, only available for exit join point |
| bool | HasException | This parameter indicates whether an exception has been thrown, only available for exit join point |

If the advice method doesn't have any parameter, Parameters element should be ignored in the configuration. In the configuration, parameter list of a method with 2 parameters of CrossCutterN.Base.Metadata.IExecutionContext and CrossCutterN.Base.Metadata.IExecution can be represented as [ "Context", "Execution" ]. In the configuration file the order of the strings doesn't matter, but in the advice assembly when defining the advice method, the parameter list must follow certain orders, if it's not empty:

| Join Point | Available Parameter and Order |
| --- | --- |
| entry | [ "Context", "Execution" ] |
| exception | [ "Context", "Execution", "Exception" ] |
| exit | [ "Context", "Execution", "Return", "HasException" ] |

:exclamation: Please note that it will be ok for any advice method to have no parameters. Besides, if certain advice method doesn't need certain parameter types, it doesn't have to have the parameter type. Only if multiple parameters exist, the order must be followed. For example, the following method definitions are all valid advice method to be injected to exit join points:

```C#
public static void Advice1(IExecution execution, bool hasException);
public static void Advice2();
public static void Advice3(IExecutionContext context);
public static void Advice4(IReturn return, bool hasException);
public static void Advice5(bool hasException);
```

:exclamation: Please note that **_CrossCutterN_** doesn't require the advice methods to be public, this allows internal or private methods to be used as advice method in case advice methods are includedin target project assemblies, or accessing private member access permission is acquired. If the target project doesn't have access to internal or private advice methods, even if injection is successful, the injected project assemblies will fail at run time when trying to call the invisible members.

### Target Assembly Configuration

The following is a complete sample of target assembly configuration:

```json
{
  "CrossCutterN": {
    "DefaultAdviceAssemblyKey": "sample",
    "AspectBuilders": {
      "aspectByAttribute": {
      	"AspectAssemblyKey": "CrossCutterN.Aspect",
        "AspectBuilderKey": "CrossCutterN.Aspect.Builder.ConcernAttributeAspectBuilder",
	"ConcernOptions": [ "Constructor", "Public", "Protected", "Internal", "Private", "Instance", "Static", "PropertySetter" ],
        "ConcernMethodAttributeType": { "AdviceAssemblyKey": "sample", "TypeKey": "method" },
        "Advices": {
          "Entry": { "AdviceAssemblyKey": "sample", "MethodKey": "attributeEntry" },
          "Exit": { "AdviceAssemblyKey": "sample", "MethodKey": "attributeExit" }
        },
        "IsSwitchedOn": true
      },
      "aspectByMethodName": {
      	"AspectAssemblyKey": "CrossCutterN.Aspect",
        "AspectBuilderKey": "CrossCutterN.Aspect.Builder.NameExpressionAspectBuilder",
	"ConcernOptions": [ "Constructor", "Public", "Protected", "Internal", "Private", "Instance", "Static", "PropertySetter" ],
        "Includes": [ "CrossCutterN.Sample.Target.Target.Ad*", "CrossCutterN.Sample.Target.Target.Add" ],
	"Excludes": [ "CrossCutterN.Sample.Target.Target.Ad1" ],
        "Advices": {
          "Entry": { "AdviceAssemblyKey": "sample", "MethodKey": "expressionEntry" },
          "Exit": { "AdviceAssemblyKey": "sample", "MethodKey": "expressionExit" }
        }
      }
    },
    "Order": {
      "Entry": [
        "aspectByAttribute",
        "aspectByMethodName"
      ],
      "Exit": [
        "aspectByMethodName",
        "aspectByAttribute"
      ]
    },
    "Targets": {
      "CrossCutterN.Sample.Target.dll": {
        "Output": "CrossCutterN.Sample.Target.Weaved.dll",
        "IncludeSymbol": true,
	"StrongNameKeyFile": "CrossCutterN.snk"
      }
    }
  }
}
```
Root element must be "CrossCutterN".
For the several sub elements contained in root element:
"DefaultAdviceAssemblyKey" is the default advice assembly key that will be applied to all advice assembly reference if not set.
"AspectBuilders" element contains a dictionary structure with key as aspect builder name and value as aspect builder configuration.
"Orders" element contains the order of injected methods to be called at each join point for a method.
"Targets" element is a dictionary structure that contains information and settings of all assemblies that are to be injected with AOP code.

#### Aspect Builder configuration

For each target assembly, all methods and properties defined in it will be scanned. All configured aspect builders in target configuration will each build an aspect, which will examin each method/property scanned to determine whether the advice methods held by the aspect should be injected into. After all aspects have examined the scanned method/property, all advice methods that are supposed to be injected will be ordered according to the "Orders" section at each join point, and then, after some advice method parameter initialization, each join point of the method/property will be injected with the advice methods.

For each aspect builder configuration, in this example, "AspectAssemblyKey" can be ignored since it's really the default CrossCutterN.Aspect assembly which provides the two default aspect builders. The key value "CrossCutterN.Aspect" is not supposed to be changed or used for custom aspect builder assemblies. "AspectAssemblyKey" element is used to match the assembly key in aspect assembly configuration, while "AspectBuilderKey" is used to index the aspect builder configured in aspect assembly configuration. With these 2 values, a unique aspect builder can be identified.

For each advice configuratoin, in this example, "AdviceAssemblyKey" can be ignored since default advice assembly key is set already and they are the same. "AdviceAssemblyKey" and "TypeKey" identifies an attribute used by ConcernAttributeAspectBuilder defined in the advice assembly, while "AdviceAssemblyKey" and "MethodKey" identifies an advice method defined in the advice assembly.

For both **ConcernAttributeAspectBuilder** and **NameExpressionAspectBuilder**, "ConcernOptions" and "SwitchStatus" are configurable elements.

All available options of "ConcernOptions" are listed below. This element may be ignored in configuration, if ignored, the values of each option will follow the default value listed below. For **ConcernAttributeAspectBuilder**, these settings **cannot** overwrite the corresponding property settings written in **CrossCutterN.Base.Concern.ConcernClassAttribute** or **CrossCutterN.Base.Concern.ConcernPropertyAttribute** attribute.

| Option | Description | Default Value |
| --- | --- | --- |
| Constructor | if not set, constructors won't be injected | false |
| Public | if not set, all public methods/properties won't be injected | true |
| Protected | if not set, all protected methods/properties  won't be injected | false |
| Internal | if not set, all internal methods/properties won't be injected | false |
| Private | if not set, all private methods/properties won't be injected | false |
| Instance | if not set, all instance methods/properties won't be injected | true |
| Static | if not set, all static methods/properties won't be injected | true |
| Method | if not set, all static methods won't be injected | true |
| PropertyGetter | if not set, all property getters won't be injected | false |
| PropertySetter | if not set, all property setters won't be injected | false |

"IsSwitchedOn" specifies the default switch status of the advice methods injected by the aspect built by the aspect builder. It's a boolean value. "true" meaning the aspects are switched on by default, "false" meaning the aspects are switched off by default. If not specified, then the aspect injected will not be switchable, which means always executed.

**ConcernAttributeAspectBuilder** has 4 attributes that can be configured, as introduced previously. For **NameExpressionAspectBuilder**, include patterns and exclude patterns can be specified, which will be used to filter methods/properties. Currently the only wild card character supported is asterisk "\*", which is used to represent 0 to any number of any characters. The way **NameExpressionAspectBuilder** uses to identify methods/properties to be injected is:

* If a method/property's full name is matched by one of the include patterns that doesn't have asterisks, it is a subject of injection.
* Else if a method/property's full name is matched by one of the exclude patterns, it is **not** a subject of injection.
* Else if a method/property's full name is matched by one of the include patterns, it is a subject of injection.
* Else the method/property is **not** a subject of injection.

#### Switching

This switching feature is used for switch on and off injected advice methods at runtime. Developers can call the **CrossCutterN.Base.Switch.IAspectSwitch** interface returned by **CrossCutterN.Base.Switch.SwitchFacade.Controller** method to to switch on and off injected advice methods at multiple granularity levels. If under some circumstances some injected advice methods shouldn't be called, and under some other circumstances they should, this switching feature is designed specifically for this purpose.

:exclamation: Please note that in one method/property getter/property setter, advice methods injected by one aspect can only be switched on or off simultaneously. For example, if aspect builder AB builds aspect A, and aspect A injects method Entry1 at entry join point, Exit1 at exit point of target method T, then if aspect A is switched off for T, then when calling T, neither Entry1 nor Exit1 will be called; If later aspect A is switched on for T, then when calling T, both Entry1 and Exit 1 will be called; There will be no way to only call Entry1 or Exit1 without calling the other when calling T, because they are both injected by aspect A, hence the switch status is the same.

:exclamation: Aspect switching feature depends on read-write lock for thread safty, comparing with non-switchable aspects its implementation may affect performance a bit and consume some extra memory resource. So please don't abuse the usage of the switching feature, only use it when necessary.

:exclamation: Aspect switching feature handles the case then when switching, some classes may not have been loaded. **CrossCutterN.Base** module will keep some optimized switching operation history internally to apply the operations once certain class is loaded, to make sure no switching operation is lost for any class or any module. This mechanism consumes extra computing resource, and contributes to the argument that the usage of aspect switching should not be abused.

#### Ordering Advice Methods

Why do we need the "Order" section? Here is an example:

**Question**: What if I have two aspects A and B, I want advices injected by A to be executed first, and B executed later?

**Answer**: We can rely on the declaring order of aspect builders A and B, declare A first, and B later, and implement the weaving process to always inject advice methods from first declared aspects first the problem is solved.

So let's say if A and B both injects at entry and exit join points, the execution of target method T will be like:
Entry injected by A -> Entry injected by B -> T -> Exit injected by A, Exit injected by B.

**Question**: what if I want the execution sequence to be like the following:
Entry injected by A -> Entry injected by B -> T -> Exit injected by B, Exit injected by A.

**Answer**: Simple ordering of aspect builders can't solve this, we need to specify the order for each join point, that's the reason we need the ordering section, for entry join point, we declare A before B, for exit join point we declare B before A.

:exclamation: Please note that if there are more than 1 aspect builders configured in the target configuration file, then "Order" section is compulsory, or else if there is only one aspect builder configured, the "Order" section can be ignored.

#### Output

In "Targets" section, for each entry of the dictionary structure:

* Key is the absolute or relevant path of the assembly that will be weaved to the configuration file. Value of the entry is the settings for weaving that assembly.
* Output is the absolute or relevant path of the weaved assembly to be output as. This configuration item is mandatory. It can be set to be the same file as the input assembly, then **_CrossCutterN_** will output the weaved assembly as the input assembly, meaning overwrite the original input assembly with weaved result.
* includeSymble meaning whether to output the symbol file (simply meaning the pdb file), if this configuration value is set to true, user should make sure that the pdb file for the weaved assembly is available in the same folder as the assembly, then pdb file for the weaved assembly will be output together with the weaved assembly. This configuration item is optional, if not specified, the value is defaulted to false.
* StrongNameKeyFile is the absolute or relevant path of strong name key file used to give the weaved assembly a strong name. This configuration item is optional. If not specified, the output assembly will not be strong named.

## Project Structure
* **CrossCutterN.Base**: Basic support assembly for **_CrossCutterN_** tool. Please make sure that this assembly is copied over to the directories where injected assemblies are deployed. It contains pre-defined advice parameters, common supports, base attributes and so on.
* **CrossCutterN.Aspect**: Contains the definition of aspect, aspect builder, implementation of **CrossCutterN.Aspect.Builder.ConcernAttributeAspectBuilder** and **CrossCutterN.Aspect.Builder.NameExpressionAspectBuilder**, plus the relevant aspect builder configuration related items. This module contains the extension point needed to implement custom aspects and aspect builders.
* **CrossCutterN.Weaver**: the module contains the weaving log using _Mono.Cecil_ IL weaving technologies, which is not supposed to be extended.
* **CrossCutterN.Console**: Console application of **_CrossCutterN_** tool to weave assemblies.
* **CrossCutterN.Test**: Unit test project.

## Usage Attention

:exclamation: Please don't use this tool to inject the already injected assemblies. There is no guarantee that it still works perfectly. 

:exclamation: There is no guarantee that CrossCutterN works with any other AOP tools.  

:exclamation: There is no point to do this AOP code injection process using multi-thread style, for developers tend to develop their own tools based on CrossCutterN source code, please be reminded that the AOP code injection part isn't designed for multi-threading at all (Though **_CrossCutterN_** doesn't prevent injection target assemblies from working correctly and provides thread safty for its aspect switching feature).  

:exclamation: There is no guarantee that CrossCutterN works with obfuscation tools.

## References

Please refer to [AOP Wikipedia](https://en.wikipedia.org/wiki/Aspect-oriented_programming) for background knowledge of AOP.

Please refer to [Mono.Cecil web site](http://www.mono-project.com/docs/tools+libraries/libraries/Mono.Cecil/) for more information about Mono.Cecil which _CrossCutterN_ depends on for IL weaving.

## Considerations:

* **Custom MsBuild Task**: Having an msbuild task certainly helps the tool to be integrated into projects much easier. The situation is that currently msbuild tool has a assembly binding redirection issue that custom msbuild tasks won't work with certain assembly binding redirection, and unfortunately **_CrossCutterN_** is one of them (mostly for the json configuration feature). Either msbuild solves this issue or **_CrossCutterN_** tries to fix the issue otherwise can this feature be provided.
* **To migrate to DotNetCore and DotNetStandard**: The source code is almost compilable in dotnet core environment except that currently some features needed from Mono.Cecil isn't completed yet, including referencing interfaces with generic patterns and outputing strong named assemblies. After that Mono.Cecil completes the relevant features, the code can be easily migrated to dotnet core and dotnet standard environments.
* **Weaver Interface Design**: Currently **CrossCutterN.Weaver.Weaver.IWeaver** interface requires file names instead of streams for input and output assemblies. This is because current Mono.Cecil support for outputing weaved assemblies with pdb files using stream completely, which leads to the current design. This can be approved after Mono.Cecil is updated.

## Contact Author

Should there be any issues, suggestions or inquiries, please submit an issue to this project. Or alternatively, send an email to keeper013@gmail.com.  
Thanks
