using Microsoft.Bot.Schema;
using Microsoft.Bot.Streaming;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.BridgeBot
{
    public class BridgeRequestHandler : RequestHandler
    {
        private readonly Action<Activity> _receiveActivities;
        public BridgeRequestHandler(Action<Activity> receiveActivities)
        {
            _receiveActivities = receiveActivities;
        }

        public override async Task<StreamingResponse> ProcessRequestAsync(ReceiveRequest request, ILogger<RequestHandler> logger = null, object context = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (request.Verb == "POST")
            {
                var firstActivity = request.ReadBodyAsJson<Activity>();

                if (request.Streams.Count > 1)
                {
                    if (firstActivity == null)
                    {
                        throw new InvalidOperationException("Attachment streams received with activity set, but no activities present in activity set.");
                    }

                    var streamAttachments = new List<Attachment>();
                    for (int i = 1; i < request.Streams.Count; i++)
                    {
                        var stream = request.Streams[i].Stream;
                        streamAttachments.Add(new Attachment() { ContentType = request.Streams[i].ContentType, Content = request.Streams[i].Stream });
                    }

                    if (firstActivity.Attachments != null)
                    {
                        firstActivity.Attachments = firstActivity.Attachments.Concat(streamAttachments).ToArray();
                    }
                    else
                    {
                        firstActivity.Attachments = streamAttachments.ToArray();
                    }
                }

                _receiveActivities(firstActivity);

                return StreamingResponse.OK();
            }
            return StreamingResponse.NotFound();
        }
    }
}
