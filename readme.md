# ARMA CS - [Downloads](https://github.com/maca134/armacs/releases)
This is an extension/mod that will allow you to compile/run C# on the fly to ease rapid development of a c# ARMA extension

The mod has only 2 functions:

Load some c#, returns a pointer
```
_pointer = [_path_to_cs] call ARMACS_fnc_load
```

Run the script and return the results
```
_result = [_pointer, _args] call ARMACS_fnc_run;
```

The c# has to implement the follow pattern so its as close as possible to the actual DllExport:
```
class Startup {
    public static void RVExtension(StringBuilder output, int outputSize, string function)
    {
        output.Append("Hello World");
    }
}
```

- All paths starting `.\` will be relative to ARMACS.dll
- Additional libraries can be loaded as follows: `#r ".\lib.dll"` or `#r "c:\path\to\lib.dll`
- Output is set to 10k
- If you use this on clients, DISABLE BATTLEYE!

## Licence
This work is licensed under Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License.

[![Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License](https://i.creativecommons.org/l/by-nc-sa/4.0/80x15.png)](http://creativecommons.org/licenses/by-nc-sa/4.0/)

If you want to use this commercially (or include it in "ARMA Samples", like my ARMA c# extension pattern) you must ask permission. *You know who you are!*