CSC = C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe
SRCS = Program.cs 
SRCS += HuDisk.cs HuBasicDiskImage.cs DataController.cs MiniOption.cs 
SRCS += DiskManager.cs DiskImage.cs HuFileEntry.cs SectorData.cs

TARGET = hudisk.exe
TARGET_PATH = ../$(TARGET)

all : $(TARGET_PATH)

$(TARGET_PATH) : $(SRCS)
	$(CSC) /out:$(TARGET) $^
	mv $(TARGET) $(TARGET_PATH)

clean : 
	rm -f $(TARGET_PATH)