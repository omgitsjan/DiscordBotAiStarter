![discordbot-omgitsjan-social-card](https://github.com/omgitsjan/DiscordBotAI/blob/master/.github/social-card.png)

# Starter Discord Bot with OpenAI Functions

This is a simple Discord bot in Dotnet that uses the OpenAI ChatGPT API to generate responses to messages in a Discord chat, also having some additional features. The bot is written in C# and uses the [DSharpPlus](https://github.com/DSharpPlus/DSharpPlus) library to interact with the Discord API and the [RestSharp](https://github.com/restsharp/RestSharp) library to make HTTP requests to the OpenAI API and for Example to the Watch2Gether API.

## Installation

To fully use this bot, you will need the following:

- A Discord bot token
- An OpenAI API key
- An Watch2Gether API key
- An OpenWeatherMap API key

Once you have these, you can clone this repository and build the project using the `dotnet` command, like this:

Copy code

`git clone https://github.com/omgitsjan/DiscordBotAI cd DiscordBot dotnet restore dotnet build`

Next, you need to specify the environment variables for the Discord bot token and ChatGPT API key when starting the bot. For example:

Dont forget to change the variables in appsettings.json. You can also optionally specify `ChatModel` (default: gpt-4o) and `ImageModel` (default: gpt-image-1).

Replace `DiscordToken`, `ChatGptApiKey`, `W2GApiKey` and `OpenWeatherMapApiKey` with your actual tokens and API keys, respectively.

## Usage

To use the bot, send a message in the form of `/chatgpt <prompt>` or `/dall-e <prompt>` where `<prompt>` is the text you want the bot to generate a response for. For example:

_ChatGPT:_

`/chatgpt What is the meaning of life?`

_Image Generation:_

`/dall-e Pixel art where monkeys trying to rob a bank`

The bot will respond with a generated image/text response based on the prompt you provided. 

By default, the bot uses **gpt-4o** for chat and **gpt-image-1** for images, but these are fully configurable in your `appsettings.json` or via environment variables (`ChatModel` and `ImageModel`).


_Watch2Gether:_

`/watch2gether (optional)<video-url>`

The bot will response with a link to a Watch2Gether Room with an optional Video as preload.

_OpenWeatherMap:_

`/weather <city>`

The bot will respond with the current Weather to a specific city.

_Ping:_

`/ping`

Retruns the Latency Pong...

## Docker Image

DiscordBotAI now has an official Docker image available on [Docker Hub](https://hub.docker.com/r/omgitsjan/discordbotai). You can use this image to easily run the Discord bot in a Docker container.

However, please note that in order to use the Docker image effectively, you need to provide your own `appsettings.json` configuration file. This file contains the necessary settings for the bot to function properly. You can find a template for the configuration file in the GitHub repository.

If you choose to use the official Docker image, make sure to mount your own `appsettings.json` file into the container or overwrite the default one with your own. This ensures that the bot has the correct configuration to run.

You can alternatively build and run the Discord bot locally using the provided `docker-compose.yml` file. This Docker Compose configuration handles the necessary build steps and setup for local development.

Please refer to the repository's README for detailed instructions on running the bot locally and using the Docker image with the required configuration. Feel free to contribute, raise issues, or submit pull requests on the GitHub repository.

## License

This project is licensed under the MIT License. See [LICENSE](https://github.com/omgitsjan/DiscordBot/blob/main/LICENSE) for details.
