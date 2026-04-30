import pymongo
client = pymongo.MongoClient('mongodb+srv://mllhzhz_db_user:M0YVBvSUMwTVvnMq@vranalysis.0uwi8nl.mongodb.net/?appName=vrAnalysis')
db = client['test']
col = db['tfg']
docs = list(col.find({'session_id': '90e3c67b-92bb-46c2-8b39-40f7e6b74524'}))
counts = {}
for d in docs:
    name = d.get('event_name', 'UNKNOWN')
    counts[name] = counts.get(name, 0) + 1
print('--- EVENT COUNTS ---')
for k, v in counts.items():
    print(k + ': ' + str(v))
print('\n--- METRIC VALUES ---')
for d in docs:
    name = d.get('event_name')
    if name in ['task_end', 'task_start', 'navigation_error', 'goal_reached', 'ui_error', 'help_event', 'inactivity_event']:
        print(name + ' -> ' + repr(d.get('event_value')))
