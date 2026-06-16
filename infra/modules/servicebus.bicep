param name string
param location string

resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: name
  location: location
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
}

// Shared access policy for the application (Send + Listen only — no Manage)
resource appAuthRule 'Microsoft.ServiceBus/namespaces/authorizationRules@2022-10-01-preview' = {
  parent: serviceBusNamespace
  name: 'TalentBridgeApp'
  properties: {
    rights: ['Send', 'Listen']
  }
}

// ── Topic: job-application-submitted ─────────────────────────────────────────
resource jobApplicationTopic 'Microsoft.ServiceBus/namespaces/topics@2022-10-01-preview' = {
  parent: serviceBusNamespace
  name: 'job-application-submitted'
  properties: {
    defaultMessageTimeToLive: 'P14D'
    enablePartitioning: false
  }
}

resource jobApplicationNotificationSub 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-10-01-preview' = {
  parent: jobApplicationTopic
  name: 'notification-sub'
  properties: { lockDuration: 'PT1M', maxDeliveryCount: 10, deadLetteringOnMessageExpiration: true }
}

resource jobApplicationEmailSub 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-10-01-preview' = {
  parent: jobApplicationTopic
  name: 'email-sub'
  properties: { lockDuration: 'PT1M', maxDeliveryCount: 10, deadLetteringOnMessageExpiration: true }
}

// ── Topic: resume-uploaded ────────────────────────────────────────────────────
resource resumeUploadedTopic 'Microsoft.ServiceBus/namespaces/topics@2022-10-01-preview' = {
  parent: serviceBusNamespace
  name: 'resume-uploaded'
  properties: {
    defaultMessageTimeToLive: 'P14D'
    enablePartitioning: false
  }
}

resource resumeProcessorSub 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-10-01-preview' = {
  parent: resumeUploadedTopic
  name: 'processor-sub'
  properties: { lockDuration: 'PT1M', maxDeliveryCount: 10, deadLetteringOnMessageExpiration: true }
}

output id string = serviceBusNamespace.id
output name string = serviceBusNamespace.name
output connectionString string = appAuthRule.listKeys().primaryConnectionString
