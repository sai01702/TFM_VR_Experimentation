import sys
import os
sys.path.append('C:\\Users\\Seyit Ahmet Inci\\Documents\\GitHub\\TFM-Sebastian\\TFG_VR_Analysis')
from python_analysis.log_parser import LogParser
from python_analysis.metrics import MetricsCalculator
import pandas as pd

parser = LogParser('mongodb+srv://mllhzhz_db_user:M0YVBvSUMwTVvnMq@vranalysis.0uwi8nl.mongodb.net/?appName=vrAnalysis', 'test', 'tfg')
logs = parser.fetch_logs({'session_id': '90e3c67b-92bb-46c2-8b39-40f7e6b74524'})
print(f'Logs loaded: {len(logs)}')
df_raw = parser.parse_logs(logs, expand_context=False)
df = parser.parse_logs(logs, expand_context=True)

configs = [entry for entry in logs if entry.get('event_type') == 'config']
configs.sort(key=lambda x: x.get('timestamp', ''))
experiment_config = configs[-1].get('event_context') if configs else {}
print(f'Session Name Config: {experiment_config.get("session", {}).get("session_name")}')

metrics = MetricsCalculator(df, experiment_config=experiment_config)
print(f'Total DF Size: {len(metrics.df)}')
print('---------')
roles = metrics.df['event_role'].value_counts()
print(roles)
print('---------')

res = metrics.compute_all()
import json
print(json.dumps(res['categorias'], indent=2))
