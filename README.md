# zef-xiso-convert
Converts original XBox ISOs in XGD format (eg redump) to the more commonly used XISO format (for Qwix, C-XBox, etc)

## What is it?

zef-xiso-convert is a command-line .NET application using the XDVDMulleter libraries to provide a quick and easy way to convert redump-style Original XBox ISOs 
(in XGD format) to the more common XISO (as it was referred to before the 360) format for tools such as C-XBox and Qwix, which on their own cannot handle the XGD redump format.

The XISO output is also smaller due to removing padding and the video partition.

## How to use it

Drag and drop a redump-style original xbox ISO over the EXE and it will create a file of the same name appended with ```[Redump Extract]```.
For example, ```test.iso``` would become ```test [Redump Extract].iso```

You can also use it on the command line and specify an output filename, eg:
```zef-xiso-convert infile.iso outfile.iso```

## More Info
This is a Visual Studio 2017 (Community Edition) project targeted at .NET Framework v4.5
