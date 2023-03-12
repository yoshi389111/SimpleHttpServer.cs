# SimpleHttpServer by C#

## Overview

This is a simple web server.

Compile with the C# compiler installed by default on Windows.

`C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe`

## How to build

Compile as below:

```
C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe SimpleHttpServer.cs
```

Alternatively you can use `build.bat` .

## Usage

```
usage: HttpServer [-p PORT] [-b ADDR] [-r DIR]
    or HttpServer [-t] [-r DIR]
```

options:

* `-p PORT` : Specify the port number to listen on. The default is 8000
* `-b ADDR` : Specify the address to bind to. The default is to accept all addresses.
* `-r DIR` : Specify the document root. The default is the current directory.
* `-t` : Use a prefix that does not require admin rights (`http://+:80/Temporary_Listen_Addresses/`)

Administrative privileges are required unless the `-t` option is used.

## License

Copyright (C) 2023 SATO, Yoshiyuki

This software is released under the MIT License. https://opensource.org/licenses/mit-license.php
