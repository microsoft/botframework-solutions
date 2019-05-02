//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
//
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
package com.microsoft.bot.solutions.speechdevices.samples.botapp;

import android.content.Context;
import android.text.format.DateUtils;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.BaseAdapter;
import android.widget.ImageView;
import android.widget.TextView;

import org.threeten.bp.DateTimeUtils;
import org.threeten.bp.format.DateTimeFormatter;

import java.text.DateFormat;
import java.text.ParseException;
import java.text.SimpleDateFormat;
import java.util.ArrayList;
import java.util.Calendar;
import java.util.Date;
import java.util.List;
import java.util.TimeZone;

import io.swagger.client.model.Activity;


public class MessageAdapter extends BaseAdapter {

    List<io.swagger.client.model.Activity> messages = new ArrayList<io.swagger.client.model.Activity>();
    Context context;

    public MessageAdapter(Context context) {
        this.context = context;
    }


    public void add(io.swagger.client.model.Activity message) {
        this.messages.add(message);
        notifyDataSetChanged();
    }

    public void clear(){
        this.messages.clear();
        notifyDataSetChanged();
    }

    @Override
    public int getCount() {
        return messages.size();
    }

    @Override
    public Object getItem(int i) {
        return messages.get(i);
    }

    @Override
    public long getItemId(int i) {
        return i;
    }

    @Override
    public View getView(int i, View convertView, ViewGroup viewGroup) {
        MessageViewHolder holder = new MessageViewHolder();
        LayoutInflater messageInflater = (LayoutInflater) context.getSystemService(android.app.Activity.LAYOUT_INFLATER_SERVICE);
        io.swagger.client.model.Activity message = messages.get(i);

        if (message.getFrom().getId().equalsIgnoreCase(Configuration.UserId)) {
            convertView = messageInflater.inflate(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.layout.item_message_sent, null);
            holder.messageBody = (TextView) convertView.findViewById(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.id.text_message_body);
            holder.timestamp = (TextView) convertView.findViewById(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.id.text_message_time);
            convertView.setTag(holder);

            try {
                holder.timestamp.setText(GetTimeSpanString(message, TimeZone.getTimeZone("PDT")));
            } catch (ParseException ex) {
                Log.i("MessageAdapter","Timestamp parsing failed, default to offset time from activity");
                holder.timestamp.setText(message.getTimestamp().format(DateTimeFormatter.ofPattern("KK:mm")));
            }
        } else {
            convertView = messageInflater.inflate(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.layout.item_message_received, null);
            holder.avatar = (ImageView) convertView.findViewById(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.id.image_message_profile);
            holder.messageBody = (TextView) convertView.findViewById(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.id.text_message_body);
            holder.timestamp = (TextView) convertView.findViewById(com.microsoft.bot.solutions.speechdevices.samples.botapp.R.id.text_message_time);
            convertView.setTag(holder);

            try {
                holder.timestamp.setText(GetTimeSpanString(message, TimeZone.getTimeZone("UTC")));
            } catch (ParseException ex) {
                Log.i("MessageAdapter","Timestamp parsing failed, default to offset time from activity");
                holder.timestamp.setText(message.getTimestamp().format(DateTimeFormatter.ofPattern("KK:mm")));
            }
        }

        holder.messageBody.setText(message.getSpeak());



        return convertView;
    }

    private String GetTimeSpanString(Activity message, TimeZone timeZone) throws ParseException {
        Calendar cal = Calendar.getInstance();
        TimeZone tz = cal.getTimeZone();

        DateFormat formatter = new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss.SSS'Z'");
        formatter.setTimeZone(timeZone);
        Date date = DateTimeUtils.toDate(message.getTimestamp().toInstant());

        long timeDiff = Calendar.getInstance().getTimeInMillis() - date.getTime();
        if(timeDiff < DateUtils.MINUTE_IN_MILLIS / 10){
            return "Just now";
        } else if (timeDiff < DateUtils.MINUTE_IN_MILLIS) {
            return DateUtils.getRelativeTimeSpanString(date.getTime(), Calendar.getInstance().getTimeInMillis(), DateUtils.SECOND_IN_MILLIS).toString();
        } else {
            return DateUtils.getRelativeTimeSpanString(date.getTime(), Calendar.getInstance().getTimeInMillis(), DateUtils.MINUTE_IN_MILLIS).toString();
        }
    }

}

class MessageViewHolder {
    public ImageView avatar;
    public TextView name;
    public TextView messageBody;
    public TextView timestamp;
}