# NOVO
Repository for NOVO-project @ HVL

Contains the (maintained) programs used for bachelor thesis "BO21E-29 NOVO SIGNAL PROCESSING AND REVERSE ENGINEERING" (https://hdl.handle.net/11250/2774935)

## NovoParser
Converts binary .dat files obtained from a Domino Ring Sampler (DRS4) and parses it into a Zip-archive full of .csv files. The Zip-archive vil have the same name as the source file, with an updated file-extension.
Multiple source files can be handled at the same time. 

__Usage:__

	NovoParser.CLI path/to/source-1.dat path/to/source-2.dat path/to/source-3.dat 
    
Note: Additional flags/arguments to control parsing behaviour can be passed as arguments. See NovoParser.ParserOptions.Options for details/implementation.

## NovoInpector
Takes Zip-archives generated by NovoParser to perform calculations on the data provided, and optionally, show plots of the data. 

__Usage:__

	python3 NovoInspector.main.py path/to/archive-1.zip path/to/archive-2.zip path/to/archive-3.zip
	
Note: Additional argumets to augment plots/calculations can be passed
