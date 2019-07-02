/*
  ESP8266 Blink by Simon Peter
  Blink the blue LED on the ESP-01 module
  This example code is in the public domain

  The blue LED on the ESP-01 module is connected to GPIO1
  (which is also the TXD pin; so we cannot use Serial.print() at the same time)

  Note that this sketch uses LED_BUILTIN to find the pin with the internal LED
*/
//#define LED 2

#include <Arduino.h>

#include <ESP8266WiFi.h>
#include <ESP8266HTTPClient.h>

//#include <JsonListener.h>

/**
 * WiFi Settings
 */
const char *ESP_HOST_NAME = "esp-" + ESP.getFlashChipId();
const char *WIFI_SSID = "<wifi_ssid>";
const char *WIFI_PASSWORD = "<wifi_password>";

// initiate the WifiClient
WiFiClient wifiClient;
HTTPClient http;

void setup()
{
  Serial.begin(74880);

  delay(500);
  connectWifi();

  pinMode(D7, OUTPUT); // Initialize the LED_BUILTIN pin as an output
  pinMode(D6, OUTPUT);
}

// the loop function runs over and over again forever
void loop()
{
  //turn on LED so we know it is working
  digitalWrite(D6, HIGH);
  delay(1000);
  digitalWrite(D6, LOW);
  delay(1000);

  Serial.printf("[WiFi Status]: %d\n\n", WiFi.status());

  //Serial.printf("[WiFi Status] : %s\n", WiFi.status());
  int count = GetWorkItemCount();

  for (int x = 0; x <= 2; x++)
  {
    for (int i = 1; i <= count; i++)
    {
      digitalWrite(D7, HIGH); // Turn the LED on (Note that LOW is the voltage level
      delay(1000);
      digitalWrite(D7, LOW);
      delay(500);
    }

    delay(3000);
  }

  delay(120000); //delay loop 10 minutes
}

int GetWorkItemCount()
{
  //Serial.println(WiFi.localIP());

  WiFiClient client;
  HTTPClient http;

  int count = 0;

  Serial.print("[HTTP] begin...\n");

  //url for your rest endpoint to get the count of github work items in the new column
  if (http.begin(client, "http://sync-github-issues-to-azure-boards.azurewebsites.net/api/workitems/new/count"))
  {
    //Serial.print("[HTTP] GET...\n");

    // start connection and send HTTP header
    int httpCode = http.GET();

    // httpCode will be negative on error
    if (httpCode > 0)
    {
      // HTTP header has been send and Server response header has been handled
      Serial.printf("[HTTP] GET... code: %d\n", httpCode);

      // file found at server
      if (httpCode == HTTP_CODE_OK || httpCode == HTTP_CODE_MOVED_PERMANENTLY)
      {
        String payload = http.getString();
        count = payload.toInt();

        Serial.printf("[HTTP] GET... count: %d\n", count);
      }
    }
    else
    {
      Serial.printf("[HTTP] GET... failed, error: %s\n", http.errorToString(httpCode).c_str());
    }

    http.end();
    Serial.print("[HTTP] end...\n");
  }
  else
  {
    Serial.printf("[HTTP} Unable to connect\n");
  }

  Serial.println("");

  return count;
}

void connectWifi()
{
  WiFi.begin(WIFI_SSID, WIFI_PASSWORD);

  Serial.print("Connecting to ");
  Serial.println(WIFI_SSID);

  while (WiFi.status() != WL_CONNECTED)
  {
    delay(500);
    Serial.print(".");
  }

  Serial.println("");
  Serial.println("WiFi connected!");
  Serial.println(WiFi.localIP());
  Serial.println();
}
