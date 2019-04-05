CSC = C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe

SRCS = Program.cs 
SRCS += HuDisk.cs HuBasicDiskImage.cs DataController.cs MiniOption.cs 
SRCS += DiskManager.cs DiskImage.cs HuFileEntry.cs SectorData.cs
SRCS += OptionType.cs

SRC_DIR = HuDisk\src

TARGET = hudisk.exe

all : $(TARGET)

$(TARGET) : $(addprefix $(SRC_DIR)\,$(SRCS))
	$(CSC) /out:$(TARGET) $^

clean : 
	rm -f $(TARGET)