import re

file_path = r'C:\Users\Yasmine\Documents\Symp\sympnet-web-service\rapport_sympnet.tex'

with open(file_path, 'r', encoding='utf-8') as f:
    lines = f.readlines()

# Locate Sprint 3 and 4 in Chapter 1
start_idx_ch1 = -1
end_idx_ch1 = -1

for i, line in enumerate(lines):
    if r"\section{Sprint 3" in line and start_idx_ch1 == -1:
        start_idx_ch1 = i
    if start_idx_ch1 != -1 and r"\section*{Conclusion}" in line:
        end_idx_ch1 = i
        break

if start_idx_ch1 != -1 and end_idx_ch1 != -1:
    sprint_3_4_lines = lines[start_idx_ch1:end_idx_ch1]
    
    # Remove from Chapter 1
    del lines[start_idx_ch1:end_idx_ch1]

    # Find the end of Chapter 3
    # Chapter 3 ends with \section*{Conclusion} after Sprint 2 evaluation
    start_idx_ch3 = -1
    for i, line in enumerate(lines):
        if r"\subsection{Évaluation du Sprint 2}" in line:
            start_idx_ch3 = i
        if start_idx_ch3 != -1 and r"\section*{Conclusion}" in line:
            # Insert right before this conclusion
            lines = lines[:i] + sprint_3_4_lines + lines[i:]
            break

with open(file_path, 'w', encoding='utf-8') as f:
    f.writelines(lines)
print('Fixed Chapter 3!')
