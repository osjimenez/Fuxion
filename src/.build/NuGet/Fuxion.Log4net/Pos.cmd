rem NET 4.5
mkdir src\.build\NuGet\Fuxion.Log4net\nuspec\lib\net45
copy  src\.build\NuGet\Fuxion.Log4net\out~\net45\Fuxion.Log4net.dll src\.build\NuGet\Fuxion.Log4net\nuspec\lib\net45\Fuxion.Log4net.dll
copy  src\.build\NuGet\Fuxion.Log4net\out~\net45\Fuxion.Log4net.pdb src\.build\NuGet\Fuxion.Log4net\nuspec\lib\net45\Fuxion.Log4net.pdb
mkdir src\.build\NuGet\Fuxion.Log4net\nuspec\tools
copy  src\.build\NuGet\Fuxion.Log4net\out~\net45\install.ps1 src\.build\NuGet\Fuxion.Log4net\nuspec\tools\install.ps1
mkdir src\.build\NuGet\Fuxion.Log4net\nuspec\content
copy  src\.build\NuGet\Fuxion.Log4net\out~\net45\log4net.config src\.build\NuGet\Fuxion.Log4net\nuspec\content\log4net.config

rem NET 4.7.2
mkdir src\.build\NuGet\Fuxion.Log4net\nuspec\lib\net472
copy  src\.build\NuGet\Fuxion.Log4net\out~\net472\Fuxion.Log4net.dll src\.build\NuGet\Fuxion.Log4net\nuspec\lib\net472\Fuxion.Log4net.dll
copy  src\.build\NuGet\Fuxion.Log4net\out~\net472\Fuxion.Log4net.pdb src\.build\NuGet\Fuxion.Log4net\nuspec\lib\net472\Fuxion.Log4net.pdb
mkdir src\.build\NuGet\Fuxion.Log4net\nuspec\tools
copy  src\.build\NuGet\Fuxion.Log4net\out~\net472\install.ps1 src\.build\NuGet\Fuxion.Log4net\nuspec\tools\install.ps1
mkdir src\.build\NuGet\Fuxion.Log4net\nuspec\content
copy  src\.build\NuGet\Fuxion.Log4net\out~\net472\log4net.config src\.build\NuGet\Fuxion.Log4net\nuspec\content\log4net.config

rem NET Standard 2.0
mkdir src\.build\NuGet\Fuxion.Log4net\nuspec\lib\netstandard2.0
copy  src\.build\NuGet\Fuxion.Log4net\out~\netstandard2.0\Fuxion.Log4net.dll src\.build\NuGet\Fuxion.Log4net\nuspec\lib\netstandard2.0\Fuxion.Log4net.dll
copy  src\.build\NuGet\Fuxion.Log4net\out~\netstandard2.0\Fuxion.Log4net.pdb src\.build\NuGet\Fuxion.Log4net\nuspec\lib\netstandard2.0\Fuxion.Log4net.pdb
copy  src\.build\NuGet\Fuxion.Log4net\out~\netstandard2.0\Fuxion.Log4net.deps.json src\.build\NuGet\Fuxion.Log4net\nuspec\lib\netstandard2.0\Fuxion.Log4net.deps.json
mkdir src\.build\NuGet\Fuxion.Log4net\nuspec\tools
copy  src\.build\NuGet\Fuxion.Log4net\out~\netstandard2.0\install.ps1 src\.build\NuGet\Fuxion.Log4net\nuspec\tools\install.ps1
mkdir src\.build\NuGet\Fuxion.Log4net\nuspec\content
copy  src\.build\NuGet\Fuxion.Log4net\out~\netstandard2.0\log4net.config src\.build\NuGet\Fuxion.Log4net\nuspec\content\log4net.config