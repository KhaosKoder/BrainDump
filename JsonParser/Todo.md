# Todo

# Comments

Add a construct to the grammer that will allow us to embed comments into the DSL.
Comments should work as follows: Start a line with "--" followed by any string 
This should have the effect of a commented line which is effectively ignored. 
We do not support block comments, the developer can just use multiple lines of "--" comments.


# Temporary variables

Currently this is our script:
```
    int x = 10;
    string y = ""hello"";
    x = x + 5;
    y = y + "" world"";
    x = x -1;
    return x;
```


I want to be able to write this:
```
    int x = 10;
    string y = ""hello"";
    string tmp:x = x + 5;
    y = y + "" world"";
    x = tmp:x -1;
    return x;
```

So, "tmp:x" should mean that this is a temporary variable. As such, it is not written into the 
underlying JObject. 

I would suggest that this can be achieved by having a second, internal JObject to store the tmp variables,
but this is just a thought - feel free to suggest better solutions. 

If we do make it an internal object, we need to be careful that when this class is being used in a multithreaded 
manner or if this interpreter is used twice - that it doesnt share the same temp-jobject between "sessions". 


I want to use this class in a very high volume system where it will be called about 3000 times 
per second - so we can't afford to waste time newing up thousands of interpreters - we need to be able to 
reuse interpreters or use them in parallel if at all possible.


# Nested variables

The json loaded into the JObject may look like this:
```
{
    "Person" : {
        "Name: : "Peter",
        "Age" : 61,
        "Address" : {
             "Line1" : "...." 
        }
    }
}
```

I need to be able to refer to Person.Name or Person.Age as variables. 

# Sub variables 

I need to be able to extract a sub object and then use it as though it was the top level object.

Something like :
``` 
    use Person {
        Name = "Jane";
    }
```

This should have the effect of changing Person.Name from Peter to Jane. 
While you are inside the {}'s of the use statement - the variables are now scoped to Person. 

And this should also work:
``` 
    use Person.Address {
        Line = "New York";
    }
```

# Arrays

Add support for arrays using the JArray backing type. 
