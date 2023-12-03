import os 


path = "C:/Users/PC/Downloads/sway.mp4" 
filename = path.split('/')[-1].split('.')[0]
os.makedirs(f"data/{filename}")
os.system(f"ffmpeg -i {path} -qscale:v 1 -qmin 1 -vf fps=2 data/{filename}/%04d.jpg")