# BookStack Import/Export script

This repository contains scripts for exporting/importing BookStack books in bulk as ZIP files.  
It is made in a C# script that runs in script [dotnet-script](https://github.com/dotnet-script/dotnet-script).  

## Scripts

The script uses the BookStack API for zip import/export.  
The repository contains the following scripts  

- `bookstack-zip-export-all.csx`
    - Export all books from the BookStack instance as a ZIP file.
    - This is saved as a file on the file system.
- `bookstack-zip-import.csx`
    - Import the book zip file into the BookStack instance.

In both scripts, the target instance and the API key to be used are specified by rewriting the configuration variables in the script head.  
Export/Import makes a large number of API requests.  
BookStack has a limit on the number of API requests per minute, but the script waits for a certain amount of time when the limit is reached and automatically continues.  

### Cautions

These scripts simply perform consecutive ZIP exports and imports.  
The features of BookStack's ZIP import/export function apply as is.  
The following points must be carefully checked.  

- Nothing will be done to the shelves.
- Nothing will be done to permissions. Imported books, chapters, and pages will be owned by the API key user.
- It does not matter if there are books with the same name. Importing twice will result in duplicates.
- Images that are not referenced in pages will not be exported.
- Comments cannot be exported/imported.

## Script Execution

The following two installations are required to run C# scripts  

1. Install the .NET SDK.
    - Scripts are compiled for execution and require the SDK, not Runtime.
    - https://dotnet.microsoft.com/download
1. Install the dotnet-script.
    - .NET is already installed, you can install it by executing the following
      ```
      dotnet tool install -g dotnet-script
      ```

If the installation is successful, the following can be performed.  
```
dotnet script <target-script-file>
```

## bookstack-zip-export-all.csx

As soon as the execution starts, the save process begins to execute, so it is necessary to rewrite the settings in the script in advance.  
When the script variable is set correctly and executed, all books from the specified instance will be saved as a ZIP file on the file system.  

## bookstack-zip-import.csx

This script also requires rewriting the settings in the script beforehand.  
When executed, it will ask you for the directory to read the import data from.  
There, specify the storage directory for the exported ZIP file.  
When a location is entered, the data is read and the entity begins to be created in the import destination instance.  

## Test environment

The directory `test-services` contains files for the docker container environment for testing the scripts.  
However, explanations are omitted. If there is anything you do not understand after looking at it, you should not use it.  

