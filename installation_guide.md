# Installation Guide

## Package Installation

This step is only necessary if you do not already have the enviroment needed  
to run the main python program.
If you do then the only additional package necessary is `pyinstaller`, install it with
```
pip install -U pyinstaller
```
after activating your enviroment.

If you do not have the necessary libaries you can import the provided enviroment file in 
`\python module\enviroment.yaml`, which contains all necessary libaries.

This should be enough but there can be some bugs with specific versions of the CUDA toolkit 
## External software

There are a number of external tools used the full list can be found in this [great tutorial](https://github.com/jonstephens85/gaussian-splatting-Windows).
There are some key differences in the placment of the tools in the conversions process.  
In the tutorial the tools are added to the `PATH` but here instead we will place them all in the `external_tools` folder. Each tool should be added to its own subfolder. The main program will look for them in the project folder bu it is still better to keep them organized.

## Compiling 

If everything was step up correctly then now you should be able to navigate to the main project folder (`GAUSSIAN-SPLATTING-GUI-PROJECT`) and call
```
pyinstaller "python module\conv_train.py"
```

This will create the executable version in the `dist\conv_train` folder

## Running 

To run the main program call 

```
dist\conv_train.py -s {path to video file}
```

