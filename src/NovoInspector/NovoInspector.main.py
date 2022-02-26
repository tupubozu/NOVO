# Revision: 2021-04-29

import os
import sys
import re
import pandas as pd
import numpy as num
import matplotlib.pyplot as mplot
from matplotlib.ticker import AutoMinorLocator
import random
import math
from multiprocessing import Pool


def getDataMatrices(dirPath):
	for root, dirs, files in os.walk(dirPath):
		return [(pd.read_csv(os.path.join(dirPath,file)),file) for file in files if os.path.splitext(file)[1] == ".csv"]

def SpotterPlot(dataMatrix, fileName):
	#dataMatrix, fileName = dataFileSet[0], dataFileSet[1]
	dataVectors = [dataMatrix[column] for column in dataMatrix.columns]

	mplot.figure(figsize=(22, 11))
	max_Y, min_Y, max_X, min_X = 10 * int((math.ceil(max([max(dataVectors[i]) for i in range(1, len(dataVectors))])) + 10) / 10), 10 * int((math.floor(min([min(dataVectors[i]) for i in range(1, len(dataVectors))])) - 10) / 10), max(dataVectors[0]), min(dataVectors[0])

	colour = ['r', 'b', 'g', 'm', 'y', 'c']
	[mplot.plot(dataVectors[0], dataVectors[i], f'{colour[(i - 1) % len(colour)]}', label=f'{dataMatrix.columns[i]}', linewidth=0.5) for i in range(1, len(dataVectors))]

	mplot.yticks(num.arange(min_Y, max_Y, 10))
	mplot.xticks(num.arange(min_X, max_X, 5))
	mplot.xlabel('Time [ns]')
	mplot.ylabel('Amplitude [mV]')
	mplot.title(f'Waveform: {fileName}')
	mplot.legend()
	mplot.grid()
	mplot.show()

	#mplot.savefig(f'{savePath}{os.sep}{fileName}.png', dpi=100)

	mplot.close() 

def HistoPlot(dataVector, header):
	print(f'Plotting histogram [{header}]...')

	n, bins, patches = mplot.hist(x=dataVector, bins='auto', color='#0504aa', alpha=0.7, rwidth=0.85)
	mplot.grid(axis='y', alpha=0.75)
	mplot.xlabel('Value')
	mplot.ylabel('Frequency')
	mplot.title(f'{header}')
	mplot.text(23, 45, r'$\mu=15, b=3$')
	maxfreq = n.max()

	mplot.ylim(ymax=num.ceil(maxfreq / 10) * 10 if maxfreq % 10 else maxfreq + 10)
	mplot.show()
	mplot.close()
	
def DataPlot(dataMatrix, header):
	fig, ax = mplot.subplots()

	[ax.plot(dataMatrix[i][0], dataMatrix[i][1][0], 'kd', linewidth=1) for i in range(0, len(dataMatrix))]
	[ax.errorbar(dataMatrix[i][0], dataMatrix[i][1][0], dataMatrix[i][1][1],0, capsize=5, elinewidth=0.3, ecolor='k') for i in range(0,len(dataMatrix))]

	x_list = [dataMatrix[i][0] for i in range(0,len(dataMatrix))]
	y_list = [dataMatrix[i][1][0] for i in range(0, len(dataMatrix))]

	z = num.polyfit(x_list, y_list, 1)
	p = num.poly1d(z)
	ax.plot(x_list, p(x_list), "r-", label = f'\u0394t(\u0394Position)', linewidth=1)

	ax.yaxis.set_minor_locator(AutoMinorLocator())
	ax.xaxis.set_minor_locator(AutoMinorLocator())
	ax.tick_params(which='major', length=7)
	ax.tick_params(which='minor', length=4)

	fig.canvas.set_window_title('\u0394Dataplot')
	mplot.xlabel('\u0394Position [cm]')
	mplot.ylabel('\u0394t [ns]')
	mplot.title(f'Dataplot: {header}')
	mplot.grid()
	mplot.legend()
	mplot.show()
	mplot.close()

def findTime(timeVector, valueVector, relativeThreshold):
	if len(timeVector) != len(valueVector):
		return 0
	else: 
		signal_absMax = max([abs(item) for item in valueVector])

		amp = relativeThreshold * signal_absMax
		t = 0
		
		for i in range(1, len(valueVector)):
			if valueVector[i - 1] <= amp < valueVector[i]:
				t = (amp - valueVector[i - 1]) * (timeVector[i] - timeVector[i - 1]) / (valueVector[i] - valueVector[i - 1]) + timeVector[i - 1]
				break
			if valueVector[i] < -amp <= valueVector[i - 1]:
				t = (-amp - valueVector[i - 1]) * (timeVector[i] - timeVector[i - 1]) / (valueVector[i] - valueVector[i - 1]) + timeVector[i - 1]
				break
				
		return t

def findTimeReverse(timeVector, valueVector, relativeThreshold):
	if len(timeVector) != len(valueVector):
		return 0
	else: 
		signal_absMax = max(abs(valueVector))

		amp = relativeThreshold * signal_absMax
		t = 0
		
		for i in range(len(valueVector) - 1, 0, -1):
			if valueVector[i - 1] <= amp < valueVector[i]:
				t = (amp - valueVector[i - 1]) * (timeVector[i] - timeVector[i - 1]) / (valueVector[i] - valueVector[i - 1]) + timeVector[i - 1]
				break
			if valueVector[i] < -amp <= valueVector[i - 1]:
				t = (-amp - valueVector[i - 1]) * (timeVector[i] - timeVector[i - 1]) / (valueVector[i] - valueVector[i - 1]) + timeVector[i - 1]
				break
				
		return t

def risetime(timeVector, valueVector):  # timeVector <= valueVector[0], valueVector <= valueVector[1+]
	t1 = findTime(timeVector, valueVector, 0.1)
	t2 = findTime(timeVector, valueVector, 0.9)

	return (t2 - t1)

def GetTimeVectors(dataMatrices):
	time_amp10_vect = []
	time_diff_amp10_vect=[]
	channel_risetime_vect=[]

	for dataMatrix in dataMatrices:
		dataVectors = [dataMatrix[column] for column in dataMatrix.columns]
		[time_amp10_vect.append(findTime(dataVectors[0], dataVectors[i],0.1)) for i in range(1,len(dataVectors))]
		if len(dataVectors) - 1 == 2:
			time_diff_amp10_vect.append(findTime(dataVectors[0], dataVectors[1],0.1) - findTime(dataVectors[0], dataVectors[2],0.1))
		elif len(dataVectors) - 1 > 2:
			print("Warning: Dataset has more than 2 channels. \nProgram is not designed to handle more than 2 channels at the moment.")
			time_diff_amp10_vect.append(findTime(dataVectors[0], dataVectors[1],0.1) - findTime(dataVectors[0], dataVectors[2],0.1))
		else:
			#print("Warning: Dataset has less than 2 channels.")
			time_diff_amp10_vect.append(0)
			
		channel_risetime_vect.extend([risetime(dataVectors[0],dataVectors[i]) for i in range(1,len(dataVectors))])
		
	return time_amp10_vect, time_diff_amp10_vect, channel_risetime_vect

def GetMaxAmplitudeVector(dataMatrices):
	AmplitudeVector = []
	for dataMatrix in dataMatrices:
		dataVectors = [dataMatrix[column] for column in dataMatrix.columns]
		AmplitudeVector.extend([max(abs(dataVectors[i])) for i in range(1,len(dataVectors))])

	return AmplitudeVector
	
def GetPulseWidthVector(dataMatrices):
	PulseWidthVector = []
	for dataMatrix in dataMatrices:
		dataVectors = [dataMatrix[column] for column in dataMatrix.columns]
		PulseWidthVector.extend([abs(findTimeReverse(dataVectors[0], dataVectors[i], 0.1) - findTime(dataVectors[0], dataVectors[i], 0.1)) for i in range(1,len(dataVectors))])

	return PulseWidthVector

def GetObjectVector(dataFileSet):
	dataMatrices, fileNames = [dataFileSet[i][0] for i in range(len(dataFileSet))], [dataFileSet[i][1] for i in range(len(dataFileSet))]
	time_amp10_list, time_diff_amp10_list, channel_risetimes = GetTimeVectors(dataMatrices)
	AmpVector, PWidthVector = GetMaxAmplitudeVector(dataMatrices), GetPulseWidthVector(dataMatrices)
		
	return dataMatrices, fileNames, time_amp10_list, time_diff_amp10_list, channel_risetimes, AmpVector, PWidthVector

def Statistics(data):
	µ_hat = sum(data) / len(data)
		
	S = math.sqrt(sum([math.pow(element - µ_hat, 2) for element in data]) / ((len(data) + 1) * len(data)))
	
	return µ_hat, S


def Main(argv):
	print("NovoInspector v1.2.7\n")
	
	if len(argv) > 0:
		file_directories = [arg for arg in argv if os.path.exists(arg)]
		for arg in argv: 
			matchObj = re.search("--pos=(.*)", arg) 
			if matchObj != None:
				temp = re.split("=", arg)
				temp = re.sub("\(|\)","",temp[1])
				strPos = re.split(",",temp)
				posVector = [float(pos) for pos in strPos if pos != '']
				if len(posVector) != len(file_directories):
					print("Length of posistion vector does not match the amount of datasets: ")
					print(posVector)
					exit()
				break
			else: 
				posVector = [i for i in range(len(file_directories))]
	else: 
		print("Pass a directory as an argument")
		exit()
		
	if len(file_directories) == 0:
		print("No valid directories found.")
		exit()
	
	if os.cpu_count() <= 2:
		threadCount = 1
	elif 2 < os.cpu_count() <= 4:
		threadCount = 2
	else:
		threadCount = os.cpu_count() - 2
	
	if threadCount <= len(file_directories):
		processCount = threadCount
	else:
		processCount = len(file_directories)
	
	
	with Pool(processes = processCount) as processPool:
		#tskIoDataOps = processPool.map_async(getDataMatrices,file_directories, 1,GetObjectVector)
		tskIoOps = processPool.map_async(getDataMatrices,file_directories, 1)
		dataCollection = processPool.map(GetObjectVector, tskIoOps.get(), 1)	   
		processPool.close()
		processPool.join()
	
	dataCoincidense = [(posVector[i],Statistics(dataCollection[i][3])) for i in range(len(dataCollection))]
	
	while True:
		try:
			ModeSelector = input(f"Select dataset ({len(dataCollection)} available, zero-indexed), data analysis[DA], or exit[EXIT]: ")
			
			try:
				dataSelector = int(ModeSelector)
			except:
				dataSelector = -1
			
			if 0 <= dataSelector < len(dataCollection):
				plotRangeIndex = [i for i in range(len(dataCollection[dataSelector][0]))]
				
				while True:
					command = input('Random plot[r], statistics[s], histograms[h] or exit[e]? \n\tCommand: ')
				
					if command == 'r':
						print('Plotting random waveform...')
						randIndex = random.randint(0, len(plotRangeIndex) - 1)
						SpotterPlot(dataCollection[dataSelector][0][plotRangeIndex[randIndex]],dataCollection[dataSelector][1][plotRangeIndex[randIndex]])
						plotRangeIndex.remove(plotRangeIndex[randIndex])
					elif command == 's':
						print(f'Dataset statistics: ')
						mean, S = Statistics(dataCollection[dataSelector][3])
						print(f'\tDifferential 10% of maximum amplitude timestamp: µ = {mean}, σ = {S}')
						mean, S = Statistics(dataCollection[dataSelector][2])
						print(f'\t10% of maximum amplitude timestamp:              µ = {mean}, σ = {S}')
						mean, S = Statistics(dataCollection[dataSelector][4])
						print(f'\tRise time:                                       µ = {mean}, σ = {S}')
						mean, S = Statistics(dataCollection[dataSelector][5])
						print(f'\tAmplitude:                                       µ = {mean}, σ = {S}')
						mean, S = Statistics(dataCollection[dataSelector][6])
						print(f'\tPulse width:                                     µ = {mean}, σ = {S}')
		
					elif command == 'h':
						HistoPlot(dataCollection[dataSelector][3], "Difference of 10% of maximum amplitude timestamps")
						HistoPlot(dataCollection[dataSelector][2], "10% of maximum amplitude timestamps")
						HistoPlot(dataCollection[dataSelector][4], "Rise time")
						HistoPlot(dataCollection[dataSelector][5], "Amplitude")
						HistoPlot(dataCollection[dataSelector][6], "Pulse width")
						
					elif command == 'e':
						break
						
					else: 
						print('Invalid input. Retry!')
			
			elif ModeSelector == 'DA':
				#print(dataCoincidense)
				if len(dataCoincidense) >= 2: 
					DataPlot(dataCoincidense, "Linear plot") # treng betre header....
				else: 
					print("Data analysis through DataPlot() is unavailable. ")
			
			elif ModeSelector == 'EXIT':
				exit()
			
			else:
				print("Invalid input. Retry!")
			
		except Exception as ex:
			print(ex)

 
if __name__ == "__main__":
	Main([sys.argv[i] for i in range(1, len(sys.argv)) if len(sys.argv) > 1])
