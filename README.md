# [Server-side] Low Latency View Streaming for Interactive Space Exploration

## Description

It is a path-walking virtual reality streaming system that apply two proposed scheme Geometric Distance Caching (GDC), Exploring Speed aware Streaming (ESS). GDC uses the geometric locality and ESS uses the characteristic of viewer's field of vision. GDC will help to reuse view data than other cache policy Least Recently Used (LRU) and Dead Reckoning (DR). ESS will send sub-view that vertically divided into four and then that will be selected by field vision and field of view. That reduce the amount of data that requested from client. Finally, Two schemes can reduce the network bandwidth and server request burden.

## Requirments

Unity3D 2018.4.28f1

## Data format

Data format is as follows

* The images must taken from grid camera structure. 
* To load and classify image with view position and head direction, Two text files are necessary. 
   -  Link_info_Region.txt
      	-  Site	Region	Start_X	End_X	Start_Y	End_Y	Theta	Start	End	
   -  Range_info_Region.txt
      -  Site	Region	Start	End	Theta



## Source code

To run this system. you have to edit **Assets/Scripts/server_driver.cs** file. You should open port number for network connection. And you should edit *Texture_port*.

~~~markdown
```c#
	private int Port = 11000;
```
~~~

