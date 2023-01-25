import os
import subprocess

folder_path = os.getcwd()

for filename in os.listdir(folder_path):
    if filename.endswith(".gif"):
        input_path = os.path.join(folder_path, filename)
        output_path = os.path.splitext(input_path)[0] + ".mp4"
        subprocess.run(["ffmpeg", "-i", input_path, "-movflags", "faststart", "-pix_fmt", "yuv420p", "-vf", "scale=trunc(iw/2)*2:trunc(ih/2)*2", output_path])