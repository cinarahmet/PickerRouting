import pandas as pd
import matplotlib.pyplot as plt
from scipy.stats import shapiro
from scipy.stats import norm
from scipy.stats import pearsonr

df = pd.DataFrame()
#df = pd.DataFrame(['ID', 'Original', 'Phased', 'PhaseGain', 'TotalGain'])

df = pd.read_csv("C:/Users/yahya.geckil/Desktop/PickerRouting/objectives.csv")

n, bins, patches = plt.hist([df['PhaseGain'],df['TotalGain']], histtype='bar', density=1)
plt.show()

n, bins, patches = plt.hist(df['PhaseGain'], histtype='bar', density=1)
plt.show()

n, bins, patches = plt.hist(df['TotalGain'], histtype='bar', density=1)
plt.show()

df[['PhaseGain', 'TotalGain']].boxplot()
mu = df[['PhaseGain', 'TotalGain']].mean()
stds = df[['PhaseGain', 'TotalGain']].std()
stat, p1 = shapiro(df['PhaseGain'])
stat, p2 = shapiro(df['TotalGain'])

print(p1,p2)
if p1>0.05:
    a = norm.interval(0.95, loc=mu[0], scale=stds[0])

if p2>0.05:    
    b = norm.interval(0.95, loc=mu[1], scale=stds[1])
    
pickLists = pd.DataFrame()
pickLists = pd.read_csv("C:/Users/yahya.geckil/Desktop/veri.csv")
plt.show()
df = pd.merge(df, pickLists, on="ID", how="left")
plt.scatter(df['Toplam_Adet'],df['PhaseGain'])
df = df[:][:-1]
r, p = pearsonr(df['Toplam_Adet'],df['PhaseGain'])