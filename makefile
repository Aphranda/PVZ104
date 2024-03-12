SHELL := CMD.EXE
OUTPUT_DIR := $(CURDIR)/executable_dir
MK_PLATFORM ?=x64

all : chcp clean restore build
	make zip

# clean project fold
clean :
	del /s /q "$(OUTPUT_DIR)"
	del /s /q "$(CURDIR)/$(MK_VERSION).zip"

# restore nuget packages
restore :
	nuget restore

# build project
build :
	MSBuild.exe PVZ104.sln -t:Rebuild -p:Configuration=$(MK_ENV);TargetFrameworkVersion=v4.8;OutDir=$(OUTPUT_DIR);Platform=$(MK_PLATFORM)

# installer called
copy_res :
	echo dll does not compile as an installation package
	exit -1

# zip project package
zip : 
	360zip.exe -ar "$(OUTPUT_DIR)" $(CURDIR)/$(MK_VERSION).zip &

chcp : 
	chcp 65001

.PHONY : all clean restore build zip