import autogen
import logging
from autogen import AssistantAgent, GroupChat, GroupChatManager, UserProxyAgent
from autogen.agentchat.contrib.gpt_assistant_agent import GPTAssistantAgent
import autogen
import os

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


current_dir = os.path.dirname(os.path.abspath(__file__))
config_list_path = os.path.join(current_dir, "OAI_CONFIG_LIST.json")
logger.info("Using config list path: %s", config_list_path)

config_list_gpt4 = autogen.config_list_from_json(
    config_list_path,
    filter_dict={"model": ["gpt-4-turbo-preview"]},
)
llm_config = {"config_list": config_list_gpt4}

user_proxy = UserProxyAgent(
    name="User_proxy",
    system_message="A human admin.",
    code_execution_config={
        "last_n_messages": 2,
        "work_dir": "groupChat",
        "use_docker": False,  # Set to False as Docker is not being used
    },
    human_input_mode="ALWAYS",
    is_termination_msg=lambda x: x.get("content", "").rstrip().endswith("TERMINATE"),
)
# Define GPTAssistantAgents with similar configuration for code execution
coder = GPTAssistantAgent(
    name="Coder",
    llm_config=llm_config,
    instructions=AssistantAgent.DEFAULT_SYSTEM_MESSAGE,
    code_execution_config={"use_docker": False}
)
analyst = GPTAssistantAgent(
    name="Data_analyst",
    instructions="You are a data analyst that offers insight into data.",
    llm_config=llm_config,
    code_execution_config={"use_docker": False})
engineer = autogen.AssistantAgent(
    name="Engineer",
    llm_config={"config_list": config_list_gpt4},
    system_message="""Engineer. You follow an approved plan. You write python/shell code to solve tasks. Wrap the 
    code in a code block that specifies the script type. The user can't modify your code. So do not suggest 
    incomplete code which requires others to modify. Don't use a code block if it's not intended to be executed by 
    the executor.Don't include multiple code blocks in one response. Do not ask others to copy and paste the result. 
    Check the execution result returned by the executor.If the result indicates there is an error, fix the error and 
    output the code again. Suggest the full code instead of partial code or code changes. If the error can't be fixed 
    or if the task is not solved even after the code is executed successfully, analyze the problem, revisit your 
    assumption, collect additional info you need, and think of a different approach to try.""",
    code_execution_config={"use_docker": False})

scientist = autogen.AssistantAgent(
    name="Scientist",
    llm_config={"config_list": config_list_gpt4},
    system_message="""Scientist. You follow an approved plan. You are able to categorize papers after seeing their 
    abstracts printed. You don't write code.""",
    code_execution_config={"use_docker": False})

planner = autogen.AssistantAgent(
    name="Planner",
    system_message="""Planner. Suggest a plan. Revise the plan based on feedback from admin and critic, until admin 
    approval.The plan may involve an engineer who can write code and a scientist who doesn't write code.Explain the 
    plan first. Be clear which step is performed by an engineer, and which step is performed by a scientist.""",
    llm_config={"config_list": config_list_gpt4},
    code_execution_config={"use_docker": False})

critic = autogen.AssistantAgent(
    name="Critic",
    system_message="Critic. Double check plan, claims, code from other agents and provide feedback. Check whether the "
                   "plan includes adding verifiable info such as source URL.",
    llm_config={"config_list": config_list_gpt4},
    code_execution_config={"use_docker": False}
)

executor = autogen.UserProxyAgent(
    name="Executor",
    system_message="Executor. Execute the code written by the engineer and report the result.",
    human_input_mode="NEVER",
    code_execution_config={
        "last_n_messages": 3,
        "work_dir": "consoleApp",
        "use_docker": False,
    },
)

group_chat = GroupChat(
    agents=[user_proxy, coder, analyst, engineer, scientist, planner, critic,executor],  # Other agents
    messages=[],
    max_round=10
)
manager = GroupChatManager(groupchat=group_chat, llm_config=llm_config, code_execution_config={"use_docker": False})


# Function to start group chat with a given message
def start_chat(message):
    user_proxy.initiate_chat(manager, message=message)


# Main loop for chat interaction
while True:
    # Accept user's question from the console
    user_question = input("\nEnter your question (or type 'exit' to quit):\n")
    if user_question.lower() == 'exit':
        break

    # Start the group chat with the user's question
    start_chat(user_question)

    # Display responses from each round of the conversation
    for round_number in range(group_chat.max_round):
        print(f"\nRound {round_number + 1}:")
        for agent in group_chat.agents:
            if agent.latest_message:
                print(f"{agent.name}: {agent.latest_message['content']}")

        # Check if conversation is terminated
        if any(agent.latest_message and agent.latest_message['content'].endswith("TERMINATE") for agent in group_chat.agents):
            break

# Reset the agents at the end
for agent in group_chat.agents:
    agent.reset()

print("\nGroup chat ended.")
