import arcpy
from arcpy import env
from arcpy.sa import *
import traceback
import sys
import time
import os

#This script estimates cost-distance for moving from one given point to another given point over a road network data.
#Caution - if the network data has error (such as island roads), the program may give an infinitely large value (or an 
#incorrect value for location where road network is of island type.
#Correct any island roads before proceeding with this script.

#As mentioned on phone, TTC relies on raster’s cell attributes and I have not yet verified what the cell attribute is. For now the script is just giving out some values that needs to be changed. I think it’s a good time for me to see and evaluate that.   
#Sara, once you copy and paste this folder somewhere, you need to change the path variable to point to correct folder. You must change the path in the script. When ArcGIS is running and project file is open, you need to copy and paste the content of script inside ‘ArcGIS’s python window’. 
#If you have opened the project files in ArcGIS, simply click the script file (or right click to open it via IDLE), and run the module (hit F5) to run the script.  
#When ArcGIS is running and you run this script from IDLE (i.e. outside of ArcGIS), the program may complain as it will find that ARCGIS has locked some file from deletion.
 
# Set local variables
stime = time.ctime()  #Start tracking time
sampMethod = "NEAREST" #Sampling method for getting raster value for each plot on cost surface
cellSize = 100 #Cell   Size in meters. Default is 100.

# Supply parameters or names [everything is assumed to be in path folder]
path="C:/Users/benktesh/Documents/Data/GIS Data/BIOSUM_FINAL"
inPlot = path+"/movedPlots.shp"
inSite = path+"/selectMovedPSites.shp" 
inRaster = path+"/road_spdcls" #grid format, for other format put file name with extension. This file must have speed class values representing seconds per km
outTable = path + "/CDTable.dbf" #this table stores the travel time to each pstites CostDistanceTable



# Set environment settings
os.chmod(path,0o777) # read/write by everyone
arcpy.env.workspace = path
arcpy.env.overwriteOutput = 1
arcpy.env.extent = "MAXOF" #Set extent to the max of inputs
arcpy.CheckOutExtension("Spatial") # Check out the ArcGIS Spatial Analyst extension license


# Clean up temp files from prev. runs
fcs = arcpy.ListRasters("CostDis_Sh*") #All temp costDistance - may not be needed
for i in range (0, len(fcs)):
    #print ('deleting ' + fcs[i])
    arcpy.Delete_management(fcs[i])
del fcs



if arcpy.Exists(outTable):
    arcpy.Delete_management(outTable)

fcs = arcpy.ListFiles("ps_*.dbf") #All individual dbf files from previous run. can do the same at the end
for i in range (0, len(fcs)):
    fn = path + "/" + fcs[i]
    arcpy.Delete_management (fn)
del fcs


# Main procedure
siteCursor = arcpy.da.SearchCursor(inSite, ["psite_id"]) #navigate through each pSite
aryCD = [] # Define variable to store cost distance table file name for each pSite and plots
siteCursor.reset() #Bring cursor at the top
for row in siteCursor:
    tempPSite = 'temp_psite'
    expression = "psite_id" + " = " + str(row[0])
    print expression
    try:
        # Execute MakeFeatureLayer
        arcpy.MakeFeatureLayer_management(inSite,tempPSite,"psite_id" + " = "+str(row[0]))
        outCostDistance = arcpy.sa.CostDistance(tempPSite, inRaster)
        outFile = path+"/ps_"+str(row[0]) + ".dbf"
        arcpy.sa.Sample(outCostDistance,inPlot,outFile,sampMethod)
        arcpy.AddField_management(outFile,"pSite_id", "TEXT")
        arcpy.CalculateField_management(outFile,"pSite_id",str(row[0]))
        aryCD.append(outFile)
        print ("    Completed creating and saving outCostDistance for psite_id: " + str(row[0]) + ". " + time.ctime()) + "\n"
        del outCostDistance
        del tempPSite
    except Exception as e:
        # If an error occurred, print line number and error message
        tb = sys.exc_info()[2]
        print(" Line {0}".format(tb.tb_lineno))
        print(e.message)
del siteCursor
del row

#print time.ctime()
#print aryCD

#merge all these tables together
arcpy.Merge_management(aryCD, outTable)
#join with inPlot table and get biosum id
arcpy.JoinField_management(outTable, inPlot.split("/")[len(inPlot.split("/"))-1].split(".")[0],inPlot,"FID",["BIOSUM_PLO"])
#Remove all other fields
arcpy.DeleteField_management(outTable,"tx_aa_plot;X;Y")

#TODO
#Change to info table
#Make script to export to Biosum's MDB file (use ODBC)

print ("I have cooled down. Process completed." + "\n Start time: " + stime + ". \n End time: " + time.ctime() + "\nEOC.")