'''
Created on Jul 29, 2013

@author: benktesh
'''

if __name__ == '__main__':
    pass

# Import system modules
import arcpy, os
import arcpy.mapping as mapping
import arcpy.da
from arcpy import env
from arcpy.sa import *
import traceback
import sys
import time

#This extension identifies the island road networks.

# Activate Any Extensions 
arcpy.CheckOutExtension("Spatial")
#any constant - to supply

#@input - inPointFeasuter - plot layer moved to the nearest road.
#@input - inRaster - road network raster data in grid format
#@input - outPointFeaters - outpoint file name

path="C:\Users\smloreno\Working_Folders\BioSum\Testing1_2\Test\GIS_layers_project\TTC"
inPointFeatures = path+"/studyPlots_moved2roads.shp"
inRaster = path+"/roads_spk"
outPointFeatures = path + "/tx_plot.shp"


mxdFile = "C:\Users\smloreno\Working_Folders\BioSum\Testing1_2\Test\gis\Test.mxd"


#any variable to initialize
arcpy.env.workspace = path;
mxd = mapping.MapDocument(mxdFile)
df = arcpy.mapping.ListDataFrames(mxd, "*")[0]
arcpy.env.overwriteOutput = 1


#CHECK PLOTS IN DISCONNECTED ROADS
#Identify plots that do not fall in on road through two steps:
#First step: Plot could fall in outside of road (due to projection error or something)
#   The raster value of road network of a plot location is checked
#   If the raster value is < 0, then those plots do not fall in the road
#   A shape file is created to store these plots and are excluded from further anlaysis
#Second step: Plot could fall on a road segment that is not connected to the rest of roads
#1: Extract a value of plot
#2: Select all the plots with raster value of < 0

# Description: Extracts the cells of a raster based on a set of points.
# Requirements: Spatial Analyst Extension
#@input - plot, psite, road
#@output - plot not on road, plot on road that are disconnected, plots on road.

# Delete Temp Files
fcs = arcpy.ListFeatureClasses("tx_plo*")
if len(fcs) > 0:
    arcpy.Delete_management(outPointFeatures)
    print 'Found existing tx_plot.shp, deleted'
else:
    print ('tx_plot.shp will be created')


# Execute ExtractValuesToPoints
ExtractValuesToPoints(inPointFeatures, inRaster, outPointFeatures, "INTERPOLATE", "VALUE_ONLY")

# Find all the plots that happens not to fall on the road network
# Store those plots  into shape file named in outPointFeature or tx_plot.shp
with arcpy.da.SearchCursor(outPointFeatures,("BIOSUM_PLO", "RASTERVALU"),'"RASTERVALU" < 0') as cursor:
    count = 0
    msg = "Plots that fall outside of the road i.e. road grid value is < 0"
    for row in sorted(cursor):
        count=count+1
        msg = msg + "\n" + str(row[0]) 
    print ('Number of plots that do not fall into the road : {0}. These plots will be saved into a text file named output.txt'.format(count))
outputFile = path + "/output.txt"
with open(outputFile, "a") as text_file:
    text_file.write(msg)
print ('Saved the lsit of plots to text file named output.txt')

print ('Completed identifying the plots not on road grid')


# # All the plots with raster value of < 0 are saved as plots that are out of road 
# outside of road grid
inFeatures = path + "/tx_plot.shp"
outFeatures = path + "/tx_or_plot.shp"

fcs = arcpy.ListFeatureClasses("tx_or_p*")
if len(fcs) > 0:
    arcpy.Delete_management(outFeatures)
    print 'Found existing tx_out_plot.shp, deleted'
else:
    print ('tx_or_plot.shp will be created')

tempLayer = "orplot"
expression = arcpy.AddFieldDelimiters(tempLayer, "RASTERVALU") + " >= 0"
 
try:
    # Execute CopyFeatures to make a new copy of the feature class
    arcpy.CopyFeatures_management(inFeatures, outFeatures)
 
    # Execute MakeFeatureLayer
    arcpy.MakeFeatureLayer_management(outFeatures, tempLayer)
 
    # Execute SelectLayerByAttribute to determine which features to delete
    arcpy.SelectLayerByAttribute_management(tempLayer, "NEW_SELECTION", expression)
 
    # Execute GetCount and if some features have been selected, then 
    #  execute DeleteFeatures to remove the selected features.
    if int(arcpy.GetCount_management(tempLayer).getOutput(0)) > 0:
        arcpy.DeleteFeatures_management(tempLayer)
    print ('tx_or_plot.shp stores plots that not on road grid. ' + time.ctime())     
except Exception as e:
    # If an error occurred, print line number and error message
    tb = sys.exc_info()[2]
    print("Line {0}".format(tb.tb_lineno))
    print(e.message)

del outFeatures

# To prepare for the second step, an out put file is created from the shape file created earlier as copy
# All the plots with raster value of < 0 are deleted, resulting on a clean set of plots that are not
# outside of road grid

inFeatures = path + "/tx_plot.shp"
outFeatures = path + "/tx_out_plot.shp"

fcs = arcpy.ListFeatureClasses("tx_out_p*")
if len(fcs) > 0:
    arcpy.Delete_management(outFeatures)
    print 'Found existing tx_out_plot.shp, deleted ' + time.ctime()
else:
    print ('tx_out_plot.shp will be created' + time.ctime())     

tempLayer = "outplot"
expression = arcpy.AddFieldDelimiters(tempLayer, "RASTERVALU") + " < 0"
 
try:
    # Execute CopyFeatures to make a new copy of the feature class
    arcpy.CopyFeatures_management(inFeatures, outFeatures)
 
    # Execute MakeFeatureLayer
    arcpy.MakeFeatureLayer_management(outFeatures, tempLayer)
 
    # Execute SelectLayerByAttribute to determine which features to delete
    arcpy.SelectLayerByAttribute_management(tempLayer, "NEW_SELECTION", expression)
 
    # Execute GetCount and if some features have been selected, then 
    #  execute DeleteFeatures to remove the selected features.
    if int(arcpy.GetCount_management(tempLayer).getOutput(0)) > 0:
        arcpy.DeleteFeatures_management(tempLayer)
    arcpy.DeleteField_management(outFeatures, ["RASTERVALU"])
    print ('tx_out_plot.shp stores plots that are on road grid'+ time.ctime())     
except Exception as e:
    # If an error occurred, print line number and error message
    import traceback
    import sys
    tb = sys.exc_info()[2]
    print("Line {0}".format(tb.tb_lineno))
    print(e.message)

#CLEAN

print ('Second step to disconnected plot segments, i.e. cost distance analysis ' + time.ctime())     
# First cost distance is created from psite
# If any cell that is not connected to psite (i..e no data value in between is then consired out of the road network
# if any plot falls on such cells then that road is the road segment that is not connected to the rest of roads
# @input file is 'pSite.shp
# @input - road grid
inSourceData = path+"/p_sites.shp"
inCostRaster = path+"/roads_spk"
maxDistance = 20000000   
outBkLinkRaster = path+"/outbklink"
fcs = arcpy.ListRasters("outbklin*")
if len(fcs) > 0:
    arcpy.Delete_management(outBkLinkRaster)
    print ('Deleted outBkLinkraster ' + time.ctime())
else:
    print ('Created outBkLinkraster ' + time.ctime())     

# Check out the ArcGIS Spatial Analyst extension license
print (path) +time.ctime()
arcpy.CheckOutExtension("Spatial")

# Execute CostDistance
outCostDistance = CostDistance(inSourceData, inCostRaster, maxDistance, outBkLinkRaster)

# Save the output
saveFile = path+"/tx_cdis"
print (saveFile)
fcs = arcpy.ListRasters("tx_cdi*")
if len(fcs) > 0:
    arcpy.Delete_management(saveFile)
    print ('Deleted cost distance raster tx_cdis ' + time.ctime())

outCostDistance.save(saveFile)
print ('Created cost distance raster tx_cdis ' + time.ctime())


# Use the plot information
inPointFeatures = path+"/tx_out_plot.shp"
inRaster = path+"/tx_cdis"
outPointFeatures = path + "/tx_dcPlot.shp"
fcs = arcpy.ListFeatureClasses("tx_dcP*")
if len(fcs) > 0:
    arcpy.Delete_management(outPointFeatures)
    print ('Deleted tx_dcPlot.shp. ' + time.ctime())
print ('Created tx_dcPlot.shp. ' + time.ctime())




# Check out the ArcGIS Spatial Analyst extension license
arcpy.CheckOutExtension("Spatial")

# Execute ExtractValuesToPoints
ExtractValuesToPoints(inPointFeatures, inRaster, outPointFeatures, "INTERPOLATE", "VALUE_ONLY")

# Find all the plots that happen not to fall on the road network
with arcpy.da.SearchCursor(outPointFeatures,("BIOSUM_PLO", "RASTERVALU"),'"RASTERVALU" < 0') as cursor:
    count = 0
    msg = "Plots that fall outside of the road i.e. road grid value is < 0"
    for row in sorted(cursor):
        count=count+1
        msg = msg + "\n" + str(row[0]) 
    print ('Number of plots that fall on disconnected road segments : {0}'.format(count))
outputFile = path + "/output.txt"
with open(outputFile, "a") as text_file:
    text_file.write(msg)
print ('Saved the lsit of plots to text file named output.txt')


# All the plots with raster value of > 0 (i.e., those falling in disconnected road segments are saved in tx_b_plot.shp
# outside of road grid
inFeatures = path + "/tx_dcplot.shp"
outFeatures = path + "/tx_b_plot.shp"

fcs = arcpy.ListFeatureClasses("tx_b_p*")
if len(fcs) > 0:
    arcpy.Delete_management(outFeatures)
    print 'Found existing tx_b_plot.shp, deleted'
else:
    print ('tx_b_plot.shp will be created')

tempLayer = "orplot"
expression = arcpy.AddFieldDelimiters(tempLayer, "RASTERVALU") + " > 0"
print (expression)
 
try:
    # Execute CopyFeatures to make a new copy of the feature class
    arcpy.CopyFeatures_management(inFeatures, outFeatures)
 
    # Execute MakeFeatureLayer
    arcpy.MakeFeatureLayer_management(outFeatures, tempLayer)
 
    # Execute SelectLayerByAttribute to determine which features to delete
    arcpy.SelectLayerByAttribute_management(tempLayer, "NEW_SELECTION", expression)
 
    # Execute GetCount and if some features have been selected, then 
    #  execute DeleteFeatures to remove the selected features.
    if int(arcpy.GetCount_management(tempLayer).getOutput(0)) > 0:
        arcpy.DeleteFeatures_management(tempLayer)
    print ('tx_b_plot.shp stores the BAD plots i.e., not on connected road grid. ' + time.ctime())     
except Exception as e:
    # If an error occurred, print line number and error message
    tb = sys.exc_info()[2]
    print("Line {0}".format(tb.tb_lineno))
    print(e.message)

# All the plots with raster value of > 0 (i.e., those falling in disconnected road segments are saved in tx_a_plot.shp
# outside of road grid
inFeatures = path + "/tx_dcplot.shp"
outFeatures = path + "/tx_a_plot.shp"

fcs = arcpy.ListFeatureClasses("tx_a_p*")
if len(fcs) > 0:
    arcpy.Delete_management(outFeatures)
    print 'Found existing tx_a_plot.shp, deleted'
else:
    print ('tx_a_plot.shp will be created')

tempLayer = "orplot"
expression = arcpy.AddFieldDelimiters(tempLayer, "RASTERVALU") + " < 0"
print (expression)
 
try:
    # Execute CopyFeatures to make a new copy of the feature class
    arcpy.CopyFeatures_management(inFeatures, outFeatures)
 
    # Execute MakeFeatureLayer
    arcpy.MakeFeatureLayer_management(outFeatures, tempLayer)
 
    # Execute SelectLayerByAttribute to determine which features to delete
    arcpy.SelectLayerByAttribute_management(tempLayer, "NEW_SELECTION", expression)
 
    # Execute GetCount and if some features have been selected, then 
    #  execute DeleteFeatures to remove the selected features.
    if int(arcpy.GetCount_management(tempLayer).getOutput(0)) > 0:
        arcpy.DeleteFeatures_management(tempLayer)
    print ('tx_a_plot.shp stores the GOOD plots i.e., on connected road grid. ' + time.ctime())     
except Exception as e:
    # If an error occurred, print line number and error message
    tb = sys.exc_info()[2]
    print("Line {0}".format(tb.tb_lineno))
    print(e.message)



#For successive analysis tx_a_plot.shp is plot layer

# print ("Shape file showing disconnected road segments is tx_dcPlot.shp") <--- ?

print ("reached the end of the code")
