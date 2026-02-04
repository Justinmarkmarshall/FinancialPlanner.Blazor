{{- define "financialplanner.name" -}}
financialplanner
{{- end -}}

{{- define "financialplanner.fullname" -}}
{{ .Release.Name }}-financialplanner
{{- end -}}

{{- define "financialplanner.labels" -}}
app.kubernetes.io/name: {{ include "financialplanner.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
app.kubernetes.io/version: {{ .Chart.AppVersion | default .Chart.Version }}
app.kubernetes.io/part-of: financialplanner
{{- end -}}
